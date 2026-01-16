using Match3.ECS.Components;
using Unity.Collections;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Spawns new tiles to fill empty cells after matches are cleared.
    /// New tiles spawn above the grid and fall into place.
    /// </summary>
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(FallSystem))]
    public partial struct FillSystem : ISystem
    {
        // Cached lists to avoid per-frame allocations
        private NativeList<(int x, int y, int spawnY)> emptyCells;
        private NativeList<(int idx, Entity tile)> newTiles;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<TimingConfig>();
            state.RequireForUpdate<ManagedReferences>();

            emptyCells = new(64, Allocator.Persistent);
            newTiles = new(64, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (emptyCells.IsCreated)
                emptyCells.Dispose();
            if (newTiles.IsCreated)
                newTiles.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameState>();
            if (gameState.Phase != GamePhase.Fill)
                return;
            
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.TileFactory == null || refs?.TileTypeRegistry == null)
                return;

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var timingConfig = SystemAPI.GetSingleton<TimingConfig>();
            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);

            // Find all empty cells, column by column
            // Track spawn Y position so tiles stack correctly above the grid
            emptyCells.Clear();
            for (int x = 0; x < gridConfig.Width; x++)
            {
                int count = 0;
                for (int y = 0; y < gridConfig.Height; y++)
                {
                    int idx = gridConfig.GetIndex(x, y);
                    if (gridCells[idx].IsEmpty)
                    {
                        // spawnY = grid height + offset for stacking
                        emptyCells.Add((x, y, gridConfig.Height + count));
                        ++count;
                    }
                }
            }

            if (emptyCells.Length == 0)
            {
                // No empty cells, go back to Fall phase for final settling
                SystemAPI.GetSingletonRW<GameState>().ValueRW.Phase = GamePhase.Fall;
                return;
            }

            // Spawn new tiles above the grid
            newTiles.Clear();
            for (int i = 0; i < emptyCells.Length; i++)
            {
                var (x, y, spawnY) = emptyCells[i];

                var type = refs.TileTypeRegistry.GetRandomType();
                var tile = refs.TileFactory.Create(x, spawnY, type);

                // Target position is the empty cell, tile will animate down
                var tileData = state.EntityManager.GetComponentData<TileData>(tile);
                tileData.GridPos = new(x, y);
                state.EntityManager.SetComponentData(tile, tileData);

                float distance = spawnY - y;
                float duration = timingConfig.FallDuration * distance;
                TileMoveHelper.Start(state.EntityManager, tile, new(x, y, 0), duration, TileState.Fall);

                newTiles.Add((gridConfig.GetIndex(x, y), tile));
            }

            // Update grid buffer with new tiles
            // Re-fetch buffer in case it was reallocated
            gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);
            foreach (var (idx, tile) in newTiles)
                gridCells[idx] = new() { Tile = tile };

            // Mark grid as dirty to update type cache
            var dirtyFlag = SystemAPI.GetSingletonRW<GridDirtyFlag>();
            dirtyFlag.ValueRW.IsDirty = true;

            SystemAPI.GetSingletonRW<GameState>().ValueRW.Phase = GamePhase.Fall;
        }
    }
}
