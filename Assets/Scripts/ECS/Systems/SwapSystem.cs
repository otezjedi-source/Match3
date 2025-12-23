using System.Threading;
using Cysharp.Threading.Tasks;
using MiniIT.CORE;
using MiniIT.ECS.Components;
using MiniIT.GAME;
using VContainer;

namespace MiniIT.ECS.Systems
{
    public class SwapSystem
    {
        [Inject] private readonly EcsWorld _world;
        [Inject] private readonly GameConfig _config;

        public async UniTask SwapTilesAsync(Entity tile1Entity, Entity tile2Entity, CancellationToken ct = default)
        {
            var tile1Comp = _world.GetComponent<TileComponent>(tile1Entity);
            var tile2Comp = _world.GetComponent<TileComponent>(tile2Entity);

            var cell1Entity = tile1Comp.CellEntity;
            var cell2Entity = tile2Comp.CellEntity;

            var cell1Comp = _world.GetComponent<CellComponent>(cell1Entity);
            var cell2Comp = _world.GetComponent<CellComponent>(cell2Entity);

            var cell1Pos = _world.GetComponent<GridPositionComponent>(cell1Entity);
            var cell2Pos = _world.GetComponent<GridPositionComponent>(cell2Entity);

            var cell1WorldPos = _world.GetComponent<WorldPositionComponent>(cell1Entity);
            var cell2WorldPos = _world.GetComponent<WorldPositionComponent>(cell2Entity);

            cell1Comp.TileEntity = tile2Entity;
            cell2Comp.TileEntity = tile1Entity;

            tile1Comp.CellEntity = cell2Entity;
            tile2Comp.CellEntity = cell1Entity;

            var tile1GridPos = _world.GetComponent<GridPositionComponent>(tile1Entity);
            var tile2GridPos = _world.GetComponent<GridPositionComponent>(tile2Entity);

            tile1GridPos.Position = cell2Pos.Position;
            tile2GridPos.Position = cell1Pos.Position;

            var tile1WorldPos = _world.GetComponent<WorldPositionComponent>(tile1Entity);
            var tile2WorldPos = _world.GetComponent<WorldPositionComponent>(tile2Entity);

            var tile1View = _world.GetComponent<ViewComponent>(tile1Entity).View as Tile;
            var tile2View = _world.GetComponent<ViewComponent>(tile2Entity).View as Tile;

            var cell1View = _world.GetComponent<ViewComponent>(cell1Entity).View as Cell;
            var cell2View = _world.GetComponent<ViewComponent>(cell2Entity).View as Cell;

            cell1View.Tile = tile2View;
            cell2View.Tile = tile1View;

            tile1View.Cell = cell2View;
            tile2View.Cell = cell1View;

            var task1 = tile1View.MoveToAsync(cell2WorldPos.Position, _config.SwapDuration, ct);
            var task2 = tile2View.MoveToAsync(cell1WorldPos.Position, _config.SwapDuration, ct);

            tile1WorldPos.Position = cell2WorldPos.Position;
            tile2WorldPos.Position = cell1WorldPos.Position;

            await UniTask.WhenAll(task1, task2);
        }
    }
}
