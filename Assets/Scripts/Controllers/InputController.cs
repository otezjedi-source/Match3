using Cysharp.Threading.Tasks;
using Match3.Core;
using Match3.Game;
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

        private Cell dragStartCell;
        private Vector3 dragStartWorldPosition;
        private bool isDragging;

        public void Init()
        {
            mainCamera = Camera.main;
        }

        public void Update()
        {
            if (!stateMachine.CanInput)
            {
                return;
            }

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
            var cell = gridController.GetCellAtWorldPosition(worldPos);

            if (cell != null && !cell.IsEmpty)
            {
                dragStartCell = cell;
                dragStartWorldPosition = worldPos;
                isDragging = true;
            }
        }

        private void HandleDrag()
        {
            var currentWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            float dragDistance = Vector3.Distance(dragStartWorldPosition, currentWorldPos);

            if (dragDistance >= config.MinDragDistance)
            {
                Vector3 dragDirection = (currentWorldPos - dragStartWorldPosition).normalized;
                Cell targetCell = GetTargetCellFromDirection(dragDirection);

                if (targetCell != null && !targetCell.IsEmpty)
                {
                    stateMachine.ProcessSwapAsync(dragStartCell, targetCell).Forget();
                    isDragging = false;
                    dragStartCell = null;
                }
            }
        }

        private void HandleDragEnd()
        {
            isDragging = false;
            dragStartCell = null;
        }

        private Cell GetTargetCellFromDirection(Vector3 direction)
        {
            if (dragStartCell == null)
            {
                return null;
            }

            int targetX = dragStartCell.Position.x;
            int targetY = dragStartCell.Position.y;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                targetX += direction.x > 0 ? 1 : -1;
            }
            else
            {
                targetY += direction.y > 0 ? 1 : -1;
            }

            return gridController.GetCell(targetX, targetY);
        }
    }
}
