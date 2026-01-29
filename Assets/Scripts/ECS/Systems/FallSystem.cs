using Match3.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Makes tiles fall down to fill empty cells.
    /// Scans columns bottom-up, moving tiles into empty spaces below them.
    /// </summary>
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(ClearCompleteSystem))]
    public partial struct FallSystem : ISystem
    {
        // Cached queries to avoid per-frame allocations
        private EntityQuery clearQuery;
        private EntityQuery movingQuery;
        private EntityQuery dropQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<TimingConfig>();
            state.RequireForUpdate<ManagedReferences>();

            clearQuery = SystemAPI.QueryBuilder().WithAll<ClearTag>().Build();
            movingQuery = SystemAPI.QueryBuilder().WithAll<TileMove>().Build();
            dropQuery = SystemAPI.QueryBuilder().WithAll<DropTag>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var phase = SystemAPI.GetSingleton<GameState>().phase;
            if (phase != GamePhase.Fall)
                return;

            // Wait for all animations to complete before processing falls
            if (!clearQuery.IsEmpty)
                return;
            if (!movingQuery.IsEmpty)
                return;
            if (!dropQuery.IsEmpty)
                return;

            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs.tileFactory == null || refs.tileTypeRegistry == null)
                return;

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var timingConfig = SystemAPI.GetSingleton<TimingConfig>();
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();

            int dropCount = DropTiles(ref state, gridCells, gridConfig, timingConfig);

            // If tiles dropped, go to Fill to spawn new ones; otherwise check for matches
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            gameState.ValueRW.phase = dropCount > 0 ? GamePhase.Fill : GamePhase.Match;
        }

        /// <summary>
        /// Scan grid for empty cells and drop tiles from above into them.
        /// Returns number of empty cells that were filled.
        /// </summary>
        private int DropTiles(
            ref SystemState state,
            DynamicBuffer<GridCell> gridCells,
            GridConfig gridConfig,
            TimingConfig timingConfig)
        {
            int count = 0;

            // Process each column independently, bottom to top
            for (int x = 0; x < gridConfig.width; x++)
            {
                for (int y = 0; y < gridConfig.height; y++)
                {
                    int idx = gridConfig.GetIndex(x, y);
                    if (!gridCells[idx].IsEmpty)
                        continue;

                    // Found empty cell - look for first tile above to drop
                    for (int yAbove = y + 1; yAbove < gridConfig.height; yAbove++)
                    {
                        int idxAbove = gridConfig.GetIndex(x, yAbove);
                        var tileAbove = gridCells[idxAbove].tile;
                        if (tileAbove == Entity.Null)
                            continue;

                        // Move tile from above into empty cell
                        gridCells[idx] = new() { tile = tileAbove };
                        gridCells[idxAbove] = new() { tile = Entity.Null };

                        // Update tile's grid position
                        var tileData = state.EntityManager.GetComponentData<TileData>(tileAbove);
                        tileData.gridPos = new(x, y);
                        state.EntityManager.SetComponentData(tileAbove, tileData);

                        // Start fall animation
                        float distance = yAbove - y;
                        float duration = timingConfig.fallDuration * distance;
                        TileMoveHelper.Start(state.EntityManager, tileAbove, new float3(x, y, 0), duration, TileState.Fall);
                        break;
                    }

                    ++count;
                }
            }
            return count;
        }
    }
}
