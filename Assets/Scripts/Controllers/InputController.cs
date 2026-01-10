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
    public class InputController : IDisposable
    {
        [Inject] private readonly GameConfig config;
        [Inject] private readonly EntityManager entityMgr;

        private Camera mainCamera;
        private EntityQuery gameStateQuery;
        private InputSystem_Actions inputActions;

        private int2 dragStartPos;
        private float3 dragStartWorldPosition;
        private bool isDragging;

        public void Init()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                throw new InvalidOperationException("Main camera not found");

            gameStateQuery = entityMgr.CreateEntityQuery(typeof(GameState));

            inputActions = new InputSystem_Actions();
            inputActions.UI.Enable();
            inputActions.UI.Click.performed += OnClick;
        }

        public void Dispose()
        {
            var worldExists = World.DefaultGameObjectInjectionWorld?.IsCreated;
            if (worldExists == true && !gameStateQuery.Equals(default))
                gameStateQuery.Dispose();

            if (inputActions != null)
            {
                inputActions.UI.Click.performed -= OnClick;
                inputActions.UI.Disable();
                inputActions.Dispose();
                inputActions = null;
            }
        }

        public void Update()
        {
            if (!CanInput())
                return;

            if (isDragging)
                HandleDrag();
        }

        private bool CanInput()
        {
            if (gameStateQuery.IsEmpty)
                return false;

            var gameState = gameStateQuery.GetSingleton<GameState>();
            return gameState.Phase == GamePhase.Idle;
        }

        private void OnClick(InputAction.CallbackContext ctx)
        {
            if (ctx.ReadValueAsButton())
                HandlePointerDown();
            else
                HandlePointerUp();
        }

        private void HandlePointerDown()
        {
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

        private void HandleDrag()
        {
            var screenPos = inputActions.UI.Point.ReadValue<Vector2>();
            float3 currentWorldPos = mainCamera.ScreenToWorldPoint(screenPos);
            float dragDistance = math.distance(dragStartWorldPosition, currentWorldPos);

            if (dragDistance >= config.MinDragDistance)
            {
                var dragDirection = math.normalize(currentWorldPos - dragStartWorldPosition);
                var targetPos = GetTargetFromDirection(dragDirection);
                if (IsValidPos(targetPos.x, targetPos.y))
                {
                    var request = entityMgr.CreateEntity();
                    entityMgr.AddComponentData<PlayerSwapRequest>(request, new() { PosA = dragStartPos, PosB = targetPos });
                    isDragging = false;
                }
            }
        }

        private void HandlePointerUp()
        {
            isDragging = false;
        }

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
        private bool IsValidPos(int x, int y) => x >= 0 && x < config.GridWidth && y >= 0 && y < config.GridHeight;
    }
}
