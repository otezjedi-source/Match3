using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MiniIT.CORE;
using MiniIT.CONTROLLERS;
using MiniIT.ECS.Components;
using MiniIT.GAME;
using VContainer;

namespace MiniIT.ECS.Systems
{
    public class FallSystem
    {
        [Inject] private readonly EcsWorld _world;
        [Inject] private readonly GameConfig _config;
        [Inject] private readonly GridInitializationSystem _gridSystem;
        [Inject] private readonly SoundController _soundController;

        public async UniTask FallTilesAsync(CancellationToken ct = default)
        {
            var movedTiles = new List<Entity>();

            for (int x = 0; x < _config.GridWidth; x++)
            {
                for (int y = 0; y < _config.GridHeight; y++)
                {
                    var cellEntity = _gridSystem.GetCellAt(x, y);
                    if (cellEntity.IsNull)
                        continue;

                    var cellComp = _world.GetComponent<CellComponent>(cellEntity);
                    if (!cellComp.TileEntity.IsNull)
                        continue;

                    for (int yAbove = y + 1; yAbove < _config.GridHeight; yAbove++)
                    {
                        var cellAboveEntity = _gridSystem.GetCellAt(x, yAbove);
                        if (cellAboveEntity.IsNull)
                            continue;

                        var cellAboveComp = _world.GetComponent<CellComponent>(cellAboveEntity);
                        if (cellAboveComp.TileEntity.IsNull)
                            continue;

                        MoveTileToCell(cellAboveComp.TileEntity, cellEntity);
                        movedTiles.Add(cellComp.TileEntity);
                        break;
                    }
                }
            }

            if (movedTiles.Count > 0)
            {
                await AnimateTilesFall(movedTiles, ct);
            }
        }

        private void MoveTileToCell(Entity tileEntity, Entity targetCellEntity)
        {
            var tileComp = _world.GetComponent<TileComponent>(tileEntity);
            var oldCellEntity = tileComp.CellEntity;

            if (!oldCellEntity.IsNull)
            {
                var oldCellComp = _world.GetComponent<CellComponent>(oldCellEntity);
                oldCellComp.TileEntity = Entity.Null;
            }

            var targetCellComp = _world.GetComponent<CellComponent>(targetCellEntity);
            targetCellComp.TileEntity = tileEntity;
            tileComp.CellEntity = targetCellEntity;

            var targetGridPos = _world.GetComponent<GridPositionComponent>(targetCellEntity);
            var tileGridPos = _world.GetComponent<GridPositionComponent>(tileEntity);
            tileGridPos.Position = targetGridPos.Position;

            var tileView = _world.GetComponent<ViewComponent>(tileEntity).View as Tile;
            var oldCellView = _world.GetComponent<ViewComponent>(oldCellEntity).View as Cell;
            var targetCellView = _world.GetComponent<ViewComponent>(targetCellEntity).View as Cell;

            if (oldCellView != null)
                oldCellView.Tile = null;

            if (targetCellView != null && tileView != null)
            {
                targetCellView.Tile = tileView;
                tileView.Cell = targetCellView;
            }
        }

        private async UniTask AnimateTilesFall(List<Entity> tileEntities, CancellationToken ct)
        {
            var moveTasks = new List<UniTask>();

            foreach (var tileEntity in tileEntities)
            {
                var tileView = _world.GetComponent<ViewComponent>(tileEntity).View as Tile;
                if (tileView != null && tileView.Cell != null)
                {
                    moveTasks.Add(tileView.MoveToAsync(tileView.Cell.transform.position, _config.FallDuration, ct));
                }
            }

            await UniTask.WhenAll(moveTasks);
            _soundController.PlayDrop();

            var dropTasks = new List<UniTask>();
            foreach (var tileEntity in tileEntities)
            {
                var tileView = _world.GetComponent<ViewComponent>(tileEntity).View as Tile;
                if (tileView != null)
                {
                    dropTasks.Add(tileView.DropAnimationAsync());
                }
            }

            await UniTask.WhenAll(dropTasks);
        }
    }
}
