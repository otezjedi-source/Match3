using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Core;
using Match3.ECS.Components;
using Match3.Factories;
using Match3.Game;
using Unity.Entities;
using Unity.Mathematics;
using VContainer;
using Random = UnityEngine.Random;

namespace Match3.Controllers
{
    public class GridController
    {
        [Inject] private readonly GameConfig config;
        [Inject] private readonly TileFactory tileFactory;
        [Inject] private readonly SoundController soundController;
        [Inject] private readonly EntityManager entityManager;

        private Entity gridEntity;

        #region Initialization
        public void Init()
        {
            CreateGridEntity();
            CreateTiles();
        }

        private void CreateGridEntity()
        {
            gridEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(gridEntity, new GridConfig
            {
                Width = config.GridWidth,
                Height = config.GridHeight
            });

            var buffer = entityManager.AddBuffer<GridCell>(gridEntity);
            buffer.Length = config.GridWidth * config.GridHeight;

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = new() { Tile = Entity.Null };
        }

        private void CreateTiles()
        {
            var tiles = new (Entity, TileType)[config.GridWidth * config.GridHeight];

            for (int x = 0; x < config.GridWidth; x++)
            {
                for (int y = 0; y < config.GridHeight; y++)
                    CreateTile(x, y, tiles);
            }

            var grid = entityManager.GetBuffer<GridCell>(gridEntity);
            for (int i = 0; i < tiles.Length; i++)
                grid[i] = new GridCell { Tile = tiles[i].Item1 };
        }

        private void CreateTile(int x, int y, (Entity, TileType)[] tiles)
        {
            var allowed = GetAllowedTypes(x, y, tiles);
            var type = allowed[Random.Range(0, allowed.Count)];
            tiles[Idx(x, y)] = (tileFactory.Create(x, y, type), type);

            List<TileType> GetAllowedTypes(int x, int y, (Entity tile, TileType type)[] tiles)
            {
                var allowed = new List<TileType>(config.TilesData.Count);
                foreach (var data in config.TilesData)
                    allowed.Add(data.type);

                if (x >= 2 && tiles[Idx(x - 1, y)].type == tiles[Idx(x - 2, y)].type)
                    allowed.Remove(tiles[Idx(x - 1, y)].type);
                
                if (y >= 2 && tiles[Idx(x, y - 1)].type == tiles[Idx(x, y - 2)].type)
                    allowed.Remove(tiles[Idx(x, y - 1)].type);

                return allowed;
            }
        }

        public void ResetTiles()
        {
            ClearTiles();
            CreateTiles();
        }

        private void ClearTiles()
        {
            var grid = entityManager.GetBuffer<GridCell>(gridEntity);
            for (int i = 0; i < grid.Length; i++)
            {
                var entity = grid[i].Tile;
                if (entity != Entity.Null)
                {
                    tileFactory.Return(entity);
                    grid[i] = new GridCell { Tile = Entity.Null };
                }
            }
        }
        #endregion

        #region Swap
        public async UniTask SwapAsync(int2 posA, int2 posB, CancellationToken ct = default)
        {
            var grid = entityManager.GetBuffer<GridCell>(gridEntity);

            int idxA = Idx(posA.x, posA.y);
            int idxB = Idx(posB.x, posB.y);

            var entityA = grid[idxA].Tile;
            var entityB = grid[idxB].Tile;

            (grid[idxA], grid[idxB]) = (grid[idxB], grid[idxA]);

            var viewA = tileFactory.GetView(entityA);
            var viewB = tileFactory.GetView(entityB);

            await UniTask.WhenAll(
                viewA.MoveToAsync(new(posB, 0), config.SwapDuration, ct),
                viewB.MoveToAsync(new(posA, 0), config.SwapDuration, ct)
            );
        }

        public void Swap(int2 posA, int2 posB)
        {
            var grid = entityManager.GetBuffer<GridCell>(gridEntity);

            int idxA = Idx(posA.x, posA.y);
            int idxB = Idx(posB.x, posB.y);

            (grid[idxA], grid[idxB]) = (grid[idxB], grid[idxA]);
        }
        #endregion

        #region Remove
        public async UniTask RemoveTilesAsync(List<int2> matches, CancellationToken ct = default)
        {
            int count = matches.Count;
            if (count == 0)
                return;

            var tasks = new UniTask[count];
            for (int i = 0; i < count; i++)
                tasks[i] = RemoveAtAsync(matches[i], ct);

            await UniTask.WhenAll(tasks);
        }

        private async UniTask RemoveAtAsync(int2 pos, CancellationToken ct = default)
        {
            var grid = entityManager.GetBuffer<GridCell>(gridEntity);
            int idx = Idx(pos.x, pos.y);
            
            var entity = grid[idx].Tile;
            if (entity == Entity.Null)
                return;

            var tile = tileFactory.GetView(entity);
            grid[idx] = new GridCell { Tile = Entity.Null };

            await tile.ClearAnimationAsync(ct);
            tileFactory.Return(entity);
        }
        #endregion

        #region Fall
        public async UniTask FallTilesAsync(CancellationToken ct = default)
        {
            var grid = entityManager.GetBuffer<GridCell>(gridEntity);
            var movedTiles = new List<(Tile, float3)>();

            for (int x = 0; x < config.GridWidth; x++)
            {
                for (int y = 0; y < config.GridHeight; y++)
                {
                    var idx = Idx(x, y);
                    if (grid[idx].Tile != Entity.Null)
                        continue;

                    for (int yAbove = y + 1; yAbove < config.GridHeight; yAbove++)
                    {
                        int idxAbove = Idx(x, yAbove);
                        var tileAbove = grid[idxAbove].Tile;
                        if (tileAbove == Entity.Null)
                            continue;

                        grid[idx] = new GridCell { Tile = tileAbove };
                        grid[idxAbove] = new() { Tile = Entity.Null };

                        var view = tileFactory.GetView(tileAbove);
                        movedTiles.Add((view, new(x, y, 0)));
                        break;
                    }
                }
            }

            if (movedTiles.Count > 0)
                await AnimateTilesFall(movedTiles, ct);
        }
        
        private async UniTask AnimateTilesFall(List<(Tile tile, float3 pos)> tiles, CancellationToken ct)
        {
            await UniTask.WhenAll(tiles.Select(t => t.tile.MoveToAsync(t.pos, config.FallDuration, ct)));
            soundController.PlayDrop();
            await UniTask.WhenAll(tiles.Select(t => t.tile.DropAnimationAsync()));
        }
        #endregion

        #region Fill
        public async UniTask FillEmptyCellsAsync(CancellationToken ct = default)
        {
            var grid = entityManager.GetBuffer<GridCell>(gridEntity);
            var newTiles = new List<(Entity entity, Tile view, int2 pos)>();

            var types = new List<TileType>(config.TilesData.Count);
            foreach (var data in config.TilesData)
                types.Add(data.type);

            for (int x = 0; x < config.GridWidth; x++)
            {
                int yOffset = 0;
                for (int y = 0; y < config.GridHeight; y++)
                {
                    if (grid[Idx(x, y)].Tile != Entity.Null)
                        continue;

                    int rnd = Random.Range(0, types.Count);
                    var entity = tileFactory.Create(x, config.GridHeight + yOffset, types[rnd]);
                    var view = tileFactory.GetView(entity);
                    newTiles.Add((entity, view, new(x, y)));
                    ++yOffset;
                }
            }

            grid = entityManager.GetBuffer<GridCell>(gridEntity);
            foreach (var (entity, _, pos) in newTiles)
            {
                int idx = Idx(pos.x, pos.y);
                grid[idx] = new GridCell { Tile = entity };
            }

            var moveViews = new List<(Tile tile, float3 pos)>();
            foreach (var (_, view, pos) in newTiles)
                moveViews.Add((view, new(pos.x, pos.y, 0)));

            if (moveViews.Count > 0)
                await AnimateTilesFall(moveViews, ct);
        }
        #endregion

        #region Helpers
        private int Idx(int x, int y) => y * config.GridWidth + x;
        
        public int2 WorldToGridPos(float3 worldPos) => new((int)math.round(worldPos.x), (int)math.round(worldPos.y));
        
        public bool IsValidPosition(int x, int y) => x >= 0 && x < config.GridWidth && y >= 0 && y < config.GridHeight;

        public TileType GetTileTypeAt(int x, int y)
        {
            var grid = entityManager.GetBuffer<GridCell>(gridEntity);
            var cell = grid[Idx(x, y)];
            if (cell.Tile == Entity.Null)
                return TileType.None;

            var tileData = entityManager.GetComponentData<TileData>(cell.Tile);
            return tileData.Type;
        }
        #endregion
    }
}
