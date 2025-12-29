using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Core;
using Match3.ECS.Components;
using Match3.Factories;
using Match3.Game;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using VContainer;
using Random = UnityEngine.Random;

namespace Match3.Controllers
{
    public class GridController : IDisposable
    {
        [Inject] private readonly GameConfig config;
        [Inject] private readonly TileFactory tileFactory;
        [Inject] private readonly TileTypesController tileTypesController;
        [Inject] private readonly SoundController soundController;
        [Inject] private readonly EntityManager entityManager;

        public event Action GridChanged;

        private Entity gridEntity;
        private NativeArray<TileType> cellTypesCache;

        public void Dispose()
        {
            if (cellTypesCache.IsCreated)
                cellTypesCache.Dispose();
        }

        #region Initialization
        public void Init()
        {
            CreateGridEntity();
            CreateTiles();
            
            cellTypesCache = new(config.GridWidth * config.GridHeight, Allocator.Persistent);
            RebuildCellTypes();
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
                var allowed = new List<TileType>();
                var types = tileTypesController.GetAllTypes();
                foreach (var t in types)
                {
                    if (x >= 2 && t == tiles[Idx(x - 1, y)].type && t == tiles[Idx(x - 2, y)].type)
                        continue;

                    if (y >= 2 && t == tiles[Idx(x, y - 1)].type && t == tiles[Idx(x, y - 2)].type)
                        continue;

                    allowed.Add(t);
                }
                return allowed;
            }
        }

        public void ResetTiles()
        {
            ClearTiles();
            CreateTiles();
            OnGridChanged();
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

            OnGridChanged();

            try
            {
                await UniTask.WhenAll(
                    viewA.MoveToAsync(new(posB, 0), config.SwapDuration, ct),
                    viewB.MoveToAsync(new(posA, 0), config.SwapDuration, ct)
                );
            } catch (OperationCanceledException)
            {
                // Swap back if cancelled
                (grid[idxA], grid[idxB]) = (grid[idxB], grid[idxA]);
                OnGridChanged();
                throw;
            }
            
        }

        public void SwapCached(int2 posA, int2 posB)
        {
            int idxA = Idx(posA.x, posA.y);
            int idxB = Idx(posB.x, posB.y);

            (cellTypesCache[idxA], cellTypesCache[idxB]) = (cellTypesCache[idxB], cellTypesCache[idxA]);
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

            OnGridChanged();

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

            OnGridChanged();

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
            var emptyCells = GetEmptyCells();
            if (emptyCells.Count == 0)
                return;

            var newTiles = CreateNewTiles();

            var grid = entityManager.GetBuffer<GridCell>(gridEntity);
            foreach (var (entity, _, pos) in newTiles)
            {
                int idx = Idx(pos.x, pos.y);
                grid[idx] = new GridCell { Tile = entity };
            }

            OnGridChanged();

            var moveViews = new List<(Tile tile, float3 pos)>();
            foreach (var (_, view, pos) in newTiles)
                moveViews.Add((view, new(pos.x, pos.y, 0)));

            if (moveViews.Count > 0)
                await AnimateTilesFall(moveViews, ct);

            Dictionary<int, List<int>> GetEmptyCells()
            {
                var grid = entityManager.GetBuffer<GridCell>(gridEntity);
                var emptyCells = new Dictionary<int, List<int>>();
                for (int x = 0; x < config.GridWidth; x++)
                {
                    var emptyRow = new List<int>();
                    for (int y = 0; y < config.GridHeight; y++)
                    {
                        if (grid[Idx(x, y)].Tile == Entity.Null)
                            emptyRow.Add(y);
                    }
                    emptyCells.Add(x, emptyRow);
                }
                return emptyCells;
            }

            List<(Entity, Tile, int2)> CreateNewTiles()
            {
                var newTiles = new List<(Entity entity, Tile view, int2 pos)>();
                foreach (var (x, yList) in emptyCells)
                {
                    for (int i = 0; i < yList.Count; i++)
                    {
                        var type = tileTypesController.GetRandomType();
                        var entity = tileFactory.Create(x, config.GridHeight + i, type);
                        var view = tileFactory.GetView(entity);
                        newTiles.Add((entity, view, new(x, yList[i])));
                    }
                }

                return newTiles;
            }
        }
        #endregion

        #region Events
        private void OnGridChanged()
        {
            RebuildCellTypes();
            GridChanged?.Invoke();
        }
        #endregion

        #region Helpers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Idx(int x, int y) => y * config.GridWidth + x;
        
        public int2 WorldToGridPos(float3 worldPos) => new((int)math.round(worldPos.x), (int)math.round(worldPos.y));
        
        public bool IsValidPosition(int x, int y) => x >= 0 && x < config.GridWidth && y >= 0 && y < config.GridHeight;

        public TileType GetTileTypeAt(int x, int y) => cellTypesCache[Idx(x, y)];
        
        private void RebuildCellTypes()
        {
            var grid = entityManager.GetBuffer<GridCell>(gridEntity);

            for (int i = 0; i < grid.Length; i++)
            {
                var tile = grid[i].Tile;
                cellTypesCache[i] = tile == Entity.Null
                    ? TileType.None
                    : entityManager.GetComponentData<TileData>(tile).Type;
            }
        }
        #endregion
    }
}
