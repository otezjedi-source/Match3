using System;
using Match3.Core;
using Match3.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Match3.Controllers
{
    public class InputController : IDisposable
    {
        [Inject] private readonly GameConfig config;
        [Inject] private readonly EntityManager entityMgr;

        private Camera mainCamera;
        private EntityQuery gameStateQuery;

        private int2 dragStartPos;
        private float3 dragStartWorldPosition;
        private bool isDragging;

        public void Init()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                throw new InvalidOperationException("Main camera not found");
            gameStateQuery = entityMgr.CreateEntityQuery(typeof(GameState));
        }

        public void Dispose()
        {
            if (!gameStateQuery.Equals(default))
                gameStateQuery.Dispose();
        }

        public void Update()
        {
            if (!CanInput())
                return;

            HandleDragInput();
        }

        private bool CanInput()
        {
            if (gameStateQuery.IsEmpty)
                return false;

            var gameState = gameStateQuery.GetSingleton<GameState>();
            return gameState.Phase == GamePhase.Idle;
        }

        private void HandleDragInput()
        {
            if (Input.GetMouseButtonDown(0))
                HandleDragBegin();

            if (isDragging && Input.GetMouseButton(0))
                HandleDrag();

            if (Input.GetMouseButtonUp(0))
                HandleDragEnd();
        }

        private void HandleDragBegin()
        {
            var worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
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
            float3 currentWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
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

        private void HandleDragEnd()
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
