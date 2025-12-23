using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MiniIT.ECS.Components;
using MiniIT.FACTORIES;
using MiniIT.GAME;
using VContainer;

namespace MiniIT.ECS.Systems
{
    public class DestroySystem
    {
        [Inject] private readonly EcsWorld _world;
        [Inject] private readonly TileFactory _tileFactory;

        public async UniTask DestroyTilesAsync(List<Entity> tileEntities)
        {
            var tasks = new List<UniTask>();

            foreach (var tileEntity in tileEntities)
            {
                tasks.Add(DestroyTileAsync(tileEntity));
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask DestroyTileAsync(Entity tileEntity)
        {
            var tileComp = _world.GetComponent<TileComponent>(tileEntity);
            var cellEntity = tileComp.CellEntity;

            if (!cellEntity.IsNull)
            {
                var cellComp = _world.GetComponent<CellComponent>(cellEntity);
                cellComp.TileEntity = Entity.Null;

                var cellView = _world.GetComponent<ViewComponent>(cellEntity).View as Cell;
                if (cellView != null)
                {
                    cellView.Tile = null;
                }
            }

            var tileView = _world.GetComponent<ViewComponent>(tileEntity).View as Tile;
            if (tileView != null)
            {
                await tileView.ClearAnimationAsync();
                tileView.Cell = null;
                _tileFactory.Return(tileView);
            }

            _world.RemoveComponent<MatchedComponent>(tileEntity);
            _world.DestroyEntity(tileEntity);
        }
    }
}
