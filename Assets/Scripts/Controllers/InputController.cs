using System;
using Match3.Core;
using Match3.ECS.Components;
using Match3.Input;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Match3.Controllers
{
    /// <summary>
    /// Handles player input and converts drag gestures into swap requests.
    /// Creates PlayerSwapRequest entities that SwapSystem processes.
    /// </summary>
    public class InputController : IDisposable
    {
        [Inject] private readonly GameConfig gameConfig;
        [Inject] private readonly EntityManager entityManager;

        private Camera mainCamera;
        private EntityQuery gameStateQuery;
        private InputSystem_Actions inputActions;

        private int2 dragStartPos;
        private float3 dragStartWorldPosition;
        private bool isDragging;

        private bool isInitialized;
        private bool isDisposed;

        public void Init()
        {
            if (isDisposed)
                throw new ObjectDisposedException("[InputController] Trying to init disposed");

            // Cleanup previous state if Init called multiple times
            DisposeQuery();
            DisposeInputActions();

            mainCamera = Camera.main;
            if (mainCamera == null)
                throw new InvalidOperationException("[InputController] Main camera not found");

            gameStateQuery = entityManager.CreateEntityQuery(typeof(GameState));

            // Setup input actions (generated from InputSystem asset)
            inputActions = new InputSystem_Actions();
            inputActions.UI.Enable();
            inputActions.UI.Click.performed += OnClick;

            isInitialized = true;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            isInitialized = false;

            DisposeQuery();
            DisposeInputActions();
        }

        private void DisposeQuery()
        {
            if (gameStateQuery.Equals(default))
                return;

            var worldExists = World.DefaultGameObjectInjectionWorld?.IsCreated == true;
            if (worldExists)
                gameStateQuery.Dispose();
            gameStateQuery = default;
        }
        
        private void DisposeInputActions()
        {
            if (inputActions == null)
                return;

            inputActions.UI.Click.performed -= OnClick;
            inputActions.UI.Disable();
            inputActions.Dispose();
            inputActions = null;
        }

        /// <summary>
        /// Called from GameInitializer.Tick() every frame.
        /// </summary>
        public void Update()
        {
            if (!isInitialized || isDisposed)
                return;

            if (isDragging && CanInput())
                HandleDrag();
        }

        /// <summary>
        /// Only allow input during Idle phase (no animations in progress).
        /// </summary>
        private bool CanInput()
        {
            if (gameStateQuery.Equals(default) || gameStateQuery.IsEmpty)
                return false;

            var gameState = gameStateQuery.GetSingleton<GameState>();
            return gameState.Phase == GamePhase.Idle;
        }

        private void OnClick(InputAction.CallbackContext ctx)
        {
            if (isDisposed || !isInitialized)
                return;

            if (ctx.ReadValueAsButton())
            {
                if (CanInput())
                    HandlePointerDown();
            }
            else
                HandlePointerUp();
        }

        private void HandlePointerDown()
        {
            if (inputActions == null || mainCamera == null)
                return;

            var screenPos = inputActions.UI.Point.ReadValue<Vector2>();
            var worldPos = mainCamera.ScreenToWorldPoint(screenPos);
            var gridPos = WorldToGridPos(worldPos);

            if (IsValidPos(gridPos.x, gridPos.y))
            {
                dragStartPos = gridPos;
                dragStartWorldPosition = worldPos;
                isDragging = true;
            }
        }

        /// <summary>
        /// While dragging, check if player has moved far enough to trigger a swap.
        /// </summary>
        private void HandleDrag()
        {
            if (inputActions == null || mainCamera == null)
                return;

            var screenPos = inputActions.UI.Point.ReadValue<Vector2>();
            float3 currentWorldPos = mainCamera.ScreenToWorldPoint(screenPos);
            float dragDistance = math.distance(dragStartWorldPosition, currentWorldPos);

            if (dragDistance >= gameConfig.MinDragDistance)
            {
                var dragDirection = math.normalize(currentWorldPos - dragStartWorldPosition);
                var targetPos = GetTargetFromDirection(dragDirection);
                if (IsValidPos(targetPos.x, targetPos.y))
                {
                    // Create swap request entity for ECS to process
                    var request = entityManager.CreateEntity();
                    entityManager.AddComponentData<PlayerSwapRequest>(request, new() { PosA = dragStartPos, PosB = targetPos });
                    isDragging = false;
                }
            }
        }

        private void HandlePointerUp()
        {
            isDragging = false;
        }

        /// <summary>
        /// Convert drag direction to target grid cell (up/down/left/right only).
        /// </summary>
        private int2 GetTargetFromDirection(float3 direction)
        {
            int targetX = dragStartPos.x;
            int targetY = dragStartPos.y;

            if (math.abs(direction.x) > math.abs(direction.y))
                targetX += direction.x > 0 ? 1 : -1;
            else
                targetY += direction.y > 0 ? 1 : -1;

            return new(targetX, targetY);
        }

        private int2 WorldToGridPos(float3 worldPos) => new((int)math.round(worldPos.x), (int)math.round(worldPos.y));
        private bool IsValidPos(int x, int y) => x >= 0 && x < gameConfig.GridWidth && y >= 0 && y < gameConfig.GridHeight;
    }
}
