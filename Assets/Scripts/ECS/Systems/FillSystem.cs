using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MiniIT.CORE;
using MiniIT.CONTROLLERS;
using MiniIT.ECS.Components;
using MiniIT.FACTORIES;
using MiniIT.GAME;
using UnityEngine;
using VContainer;

namespace MiniIT.ECS.Systems
{
    public class FillSystem
    {
        [Inject] private readonly EcsWorld _world;
        [Inject] private readonly GameConfig _config;
        [Inject] private readonly GridInitializationSystem _gridSystem;
        [Inject] private readonly TileFactory _tileFactory;
        [Inject] private readonly SoundController _soundController;

        public async UniTask FillEmptyCellsAsync(CancellationToken ct = default)
        {
            var newTileEntities = new List<Entity>();

            for (int x = 0; x < _config.GridWidth; x++)
            {
                FillColumn(x, newTileEntities);
            }

            if (newTileEntities.Count > 0)
            {
                await AnimateTilesFall(newTileEntities, ct);
            }
        }

        private void FillColumn(int x, List<Entity> newTiles)
        {
            int emptyCount = CountEmptyInColumn(x);

            for (int i = 0; i < emptyCount; i++)
            {
                int targetY = _config.GridHeight - emptyCount + i;
                var cellEntity = _gridSystem.GetCellAt(x, targetY);
                if (cellEntity.IsNull)
                    continue;

                var spawnPos = new Vector3(x, _config.GridHeight + i, 0);
                var tileView = _tileFactory.Create(spawnPos);

                var tileEntity = CreateTileEntity(cellEntity, tileView.Type, new Vector2Int(x, targetY), spawnPos, tileView);
                tileView.Entity = tileEntity;

                var cellComp = _world.GetComponent<CellComponent>(cellEntity);
                cellComp.TileEntity = tileEntity;

                var cellView = _world.GetComponent<ViewComponent>(cellEntity).View as Cell;
                if (cellView != null)
                {
                    cellView.Tile = tileView;
                    tileView.Cell = cellView;
                }

                newTiles.Add(tileEntity);
            }
        }

        private int CountEmptyInColumn(int x)
        {
            int count = 0;
            for (int y = 0; y < _config.GridHeight; y++)
            {
                var cellEntity = _gridSystem.GetCellAt(x, y);
                if (cellEntity.IsNull)
                    continue;

                var cellComp = _world.GetComponent<CellComponent>(cellEntity);
                if (cellComp.TileEntity.IsNull)
                    count++;
            }
            return count;
        }

        private Entity CreateTileEntity(Entity cellEntity, TileType type, Vector2Int gridPos, Vector3 worldPos, Tile tileView)
        {
            var entity = _world.CreateEntity();

            var tileTypeComp = _world.AddComponent<TileTypeComponent>(entity);
            tileTypeComp.Type = type;

            var tileComp = _world.AddComponent<TileComponent>(entity);
            tileComp.CellEntity = cellEntity;

            var gridPosComp = _world.AddComponent<GridPositionComponent>(entity);
            gridPosComp.Position = gridPos;

            var worldPosComp = _world.AddComponent<WorldPositionComponent>(entity);
            worldPosComp.Position = worldPos;

            var viewComp = _world.AddComponent<ViewComponent>(entity);
            viewComp.View = tileView;

            return entity;
        }

        private async UniTask AnimateTilesFall(List<Entity> tileEntities, CancellationToken ct)
        {
            var moveTasks = new List<UniTask>();

            foreach (var tileEntity in tileEntities)
            {
                var tileView = _world.GetComponent<ViewComponent>(tileEntity).View as Tile;
                if (tileView != null && tileView.Cell != null)
                {
                    var targetPos = tileView.Cell.transform.position;
                    moveTasks.Add(tileView.MoveToAsync(targetPos, _config.FallDuration, ct));

                    var worldPosComp = _world.GetComponent<WorldPositionComponent>(tileEntity);
                    worldPosComp.Position = targetPos;
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
