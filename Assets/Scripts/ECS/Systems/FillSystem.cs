using Match3.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
        private struct EmptyCell
        {
            public int2 pos;
            public int spawnY;
        }

        private struct NewTile
        {
            public int idx;
            public Entity tile;
        }
        
        // Cached lists to avoid per-frame allocations
        private NativeList<EmptyCell> emptyCells;
        private NativeList<NewTile> newTiles;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
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
            if (gameState.phase != GamePhase.Fill)
                return;
            
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs.tileFactory == null || refs.tileTypeRegistry == null)
                return;

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var timingConfig = SystemAPI.GetSingleton<TimingConfig>();
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();

            // Find all empty cells, column by column
            // Track spawn Y position so tiles stack correctly above the grid
            emptyCells.Clear();
            for (int x = 0; x < gridConfig.width; x++)
            {
                int count = 0;
                for (int y = 0; y < gridConfig.height; y++)
                {
                    int idx = gridConfig.GetIndex(x, y);
                    if (!gridCells[idx].IsEmpty)
                        continue;
                    
                    emptyCells.Add(new()
                    {
                        pos = new(x, y),
                        spawnY = gridConfig.height + count
                    });
                        
                    ++count;
                }
            }

            if (emptyCells.Length == 0)
            {
                // No empty cells, go back to Fall phase for final settling
                SystemAPI.GetSingletonRW<GameState>().ValueRW.phase = GamePhase.Fall;
                return;
            }

            // Spawn new tiles above the grid
            newTiles.Clear();
            for (int i = 0; i < emptyCells.Length; i++)
            {
                var cell = emptyCells[i];

                var type = refs.tileTypeRegistry.GetRandomType();
                var tile = refs.tileFactory.Create(cell.pos.x, cell.spawnY, type);

                // Target position is the empty cell, tile will animate down
                var tileData = state.EntityManager.GetComponentData<TileData>(tile);
                tileData.gridPos = cell.pos;
                state.EntityManager.SetComponentData(tile, tileData);

                float distance = cell.spawnY - cell.pos.y;
                float duration = timingConfig.fallDuration * distance;
                TileMoveHelper.Start(state.EntityManager, tile, new(cell.pos.x, cell.pos.y, 0), duration, TileState.Fall);

                newTiles.Add(new()
                {
                    idx = gridConfig.GetIndex(cell.pos),
                    tile = tile
                });
            }

            // Update grid buffer with new tiles
            // Re-fetch buffer in case it was reallocated
            gridCells = SystemAPI.GetSingletonBuffer<GridCell>();
            foreach (var tile in newTiles)
                gridCells[tile.idx] = new() { tile = tile.tile };

            // Mark grid as dirty to update type cache
            SystemAPI.GetSingletonRW<GridDirtyFlag>().ValueRW.isDirty = true;
            SystemAPI.GetSingletonRW<GameState>().ValueRW.phase = GamePhase.Fall;
        }
    }
}
