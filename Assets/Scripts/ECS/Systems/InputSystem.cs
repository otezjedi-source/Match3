using Cysharp.Threading.Tasks;
using MiniIT.CORE;
using MiniIT.ECS.Components;
using MiniIT.GAME;
using UnityEngine;
using VContainer;

namespace MiniIT.ECS.Systems
{
    public class InputSystem
    {
        [Inject] private readonly EcsWorld _world;
        [Inject] private readonly GridInitializationSystem _gridSystem;
        [Inject] private readonly GameStateMachine _stateMachine;
        [Inject] private readonly GameConfig _config;

        private Camera _mainCamera;
        private Entity _dragStartCellEntity;
        private Vector3 _dragStartWorldPosition;
        private bool _isDragging;

        public void Init()
        {
            _mainCamera = Camera.main;
        }

        public void Update()
        {
            if (!_stateMachine.CanInput)
                return;

            HandleDragInput();
        }

        private void HandleDragInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleDragBegin();
            }

            if (_isDragging && Input.GetMouseButton(0))
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
            var worldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var cellEntity = GetCellAtWorldPosition(worldPos);

            if (!cellEntity.IsNull)
            {
                var cellComp = _world.GetComponent<CellComponent>(cellEntity);
                if (!cellComp.TileEntity.IsNull)
                {
                    _dragStartCellEntity = cellEntity;
                    _dragStartWorldPosition = worldPos;
                    _isDragging = true;
                }
            }
        }

        private void HandleDrag()
        {
            var currentWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            float dragDistance = Vector3.Distance(_dragStartWorldPosition, currentWorldPos);

            if (dragDistance >= _config.MinDragDistance)
            {
                Vector3 dragDirection = (currentWorldPos - _dragStartWorldPosition).normalized;
                Entity targetCellEntity = GetTargetCellFromDirection(dragDirection);

                if (!targetCellEntity.IsNull)
                {
                    var targetCellComp = _world.GetComponent<CellComponent>(targetCellEntity);
                    if (!targetCellComp.TileEntity.IsNull)
                    {
                        _stateMachine.ProcessSwapAsync(_dragStartCellEntity, targetCellEntity).Forget();
                        _isDragging = false;
                        _dragStartCellEntity = Entity.Null;
                    }
                }
            }
        }

        private void HandleDragEnd()
        {
            _isDragging = false;
            _dragStartCellEntity = Entity.Null;
        }

        private Entity GetTargetCellFromDirection(Vector3 direction)
        {
            if (_dragStartCellEntity.IsNull)
                return Entity.Null;

            var gridPos = _world.GetComponent<GridPositionComponent>(_dragStartCellEntity);
            int targetX = gridPos.Position.x;
            int targetY = gridPos.Position.y;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                targetX += direction.x > 0 ? 1 : -1;
            }
            else
            {
                targetY += direction.y > 0 ? 1 : -1;
            }

            return _gridSystem.GetCellAt(targetX, targetY);
        }

        private Entity GetCellAtWorldPosition(Vector3 worldPosition)
        {
            int x = Mathf.RoundToInt(worldPosition.x);
            int y = Mathf.RoundToInt(worldPosition.y);
            return _gridSystem.GetCellAt(x, y);
        }
    }
}
