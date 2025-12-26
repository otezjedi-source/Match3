using Cysharp.Threading.Tasks;
using Match3.Core;
using Match3.Game;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Match3.Controllers
{
    public class InputController
    {
        [Inject] private readonly GridController gridController;
        [Inject] private readonly GameStateMachine stateMachine;
        [Inject] private readonly GameConfig config;
        
        private Camera mainCamera;

        private int2 dragStartPos;
        private float3 dragStartWorldPosition;
        private bool isDragging;

        public void Init()
        {
            mainCamera = Camera.main;
        }

        public void Update()
        {
            if (!stateMachine.CanInput)
                return;

            HandleDragInput();
        }

        private void HandleDragInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleDragBegin();
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                HandleDrag();
            }

            if (Input.GetMouseButtonUp(0))
            {
                HandleDragEnd();
            }
        }

        private void HandleDragBegin()
        {
            var worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var gridPos = gridController.WorldToGridPos(worldPos);
            if (gridController.IsValidPosition(gridPos.x, gridPos.y))
            {
                dragStartPos = gridPos;
                dragStartWorldPosition = worldPos;
                isDragging = true;
            }
        }

        private void HandleDrag()
        {
            float3 currentWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            float dragDistance = Vector3.Distance(dragStartWorldPosition, currentWorldPos);

            if (dragDistance >= config.MinDragDistance)
            {
                var dragDirection = math.normalize(currentWorldPos - dragStartWorldPosition);
                var targetPos = GetTargetFromDirection(dragDirection);
                if (gridController.IsValidPosition(targetPos.x, targetPos.y))
                {
                    stateMachine.ProcessSwapAsync(dragStartPos, targetPos).Forget();
                    isDragging = false;
                }
            }
        }

        private void HandleDragEnd()
        {
            isDragging = false;
        }

        private int2 GetTargetFromDirection(Vector3 direction)
        {
            int targetX = dragStartPos.x;
            int targetY = dragStartPos.y;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                targetX += direction.x > 0 ? 1 : -1;
            else
                targetY += direction.y > 0 ? 1 : -1;

            return new(targetX, targetY);
        }
    }
}
