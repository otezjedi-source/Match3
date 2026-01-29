using Match3.ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Scans the grid for matching tiles (3+ in a row/column).
    /// Runs after swap animation completes. If no matches found, reverts the swap.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(SwapCompleteSystem))]
    public partial struct MatchSystem : ISystem
    {
        private NativeHashSet<int2> matchesCache;
        private ComponentLookup<MatchTag> matchLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<MatchConfig>();
            state.RequireForUpdate<TimingConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            matchesCache = new(64, Allocator.Persistent);
            matchLookup = SystemAPI.GetComponentLookup<MatchTag>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (matchesCache.IsCreated)
                matchesCache.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            if (gameState.ValueRO.phase != GamePhase.Match)
                return;
            
            matchLookup.Update(ref state);

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var matchConfig = SystemAPI.GetSingleton<MatchConfig>();
            var timingConfig = SystemAPI.GetSingleton<TimingConfig>();
            var typeCache = SystemAPI.GetSingletonBuffer<GridTileTypeCache>();
            var matchResults = SystemAPI.GetSingletonBuffer<MatchResult>();
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();
            
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Find all matches using line scanning algorithm
            matchesCache.Clear();
            if (matchesCache.Capacity < gridConfig.CellCount)
                matchesCache.Capacity = gridConfig.CellCount;

            ScanLines(matchesCache, typeCache, gridConfig, matchConfig, true);
            ScanLines(matchesCache, typeCache, gridConfig, matchConfig, false);

            // Store results in buffer for other systems to use
            matchResults.Clear();
            foreach (var pos in matchesCache)
                matchResults.Add(new() { pos = pos });

            if (matchResults.Length > 0)
            {
                // Matches found - proceed to clear phase
                MarkMatchedTiles(ecb, gridCells, matchResults, gridConfig);

                gameState = SystemAPI.GetSingletonRW<GameState>();
                gameState.ValueRW.phase = GamePhase.Clear;
                gameState.ValueRW.phaseTimer = timingConfig.matchDelay;
                return;
            }

            // No matches - check if we need to revert the swap
            gridCells = SystemAPI.GetSingletonBuffer<GridCell>();
            
            bool isReverting = HandleSwaps(ref state, ecb, gridCells, gridConfig, timingConfig);
            if (isReverting)
                SystemAPI.GetSingletonRW<GridDirtyFlag>().ValueRW.isDirty = true;
            
            gameState.ValueRW.phase = isReverting ? GamePhase.Swap : GamePhase.Idle;
        }

        /// <summary>
        /// Scans all lines (rows or columns) for sequences of matching tiles.
        /// Uses run-length encoding approach: find start of sequence, extend until type changes.
        /// </summary>
        [BurstCompile]
        private readonly void ScanLines(
            NativeHashSet<int2> matches,
            DynamicBuffer<GridTileTypeCache> typeCache,
            GridConfig gridConfig, MatchConfig matchConfig,
            bool horizontal)
        {
            int linesCount = horizontal ? gridConfig.height : gridConfig.width;
            int lineLength = horizontal ? gridConfig.width : gridConfig.height;

            for (int lineIdx = 0; lineIdx < linesCount; lineIdx++)
            {
                int i = 0;
                while (i < lineLength)
                {
                    int x = horizontal ? i : lineIdx;
                    int y = horizontal ? lineIdx : i;
                    int idx = gridConfig.GetIndex(x, y);

                    var type = typeCache[idx].type;
                    if (type == TileType.None)
                    {
                        i++;
                        continue;
                    }

                    // Find end of matching sequence
                    int j = i + 1;
                    while (j < lineLength)
                    {
                        int nx = horizontal ? j : lineIdx;
                        int ny = horizontal ? lineIdx : j;
                        int nidx = gridConfig.GetIndex(nx, ny);

                        if (typeCache[nidx].type != type)
                            break;

                        j++;
                    }

                    // If sequence is long enough, mark all positions as matched
                    if (j - i >= matchConfig.matchCount)
                    {
                        for (int k = i; k < j; k++)
                            matches.Add(horizontal ? new(k, lineIdx) : new(lineIdx, k));
                    }

                    i = j;
                }
            }
        }

        private void MarkMatchedTiles(
            EntityCommandBuffer ecb,
            DynamicBuffer<GridCell> gridCells,
            DynamicBuffer<MatchResult> matches,
            GridConfig gridConfig)
        {
            foreach (var match in matches)
            {
                int idx = gridConfig.GetIndex(match.pos);
                var tile = gridCells[idx].tile;
                if (tile != Entity.Null && !matchLookup.HasComponent(tile))
                    ecb.AddComponent<MatchTag>(tile);
            }
        }

        /// <summary>
        /// If no matches were found after a swap, revert the swap animation.
        /// </summary>
        private bool HandleSwaps(
            ref SystemState state,
            EntityCommandBuffer ecb,
            DynamicBuffer<GridCell> gridCells,
            GridConfig gridConfig,
            TimingConfig timingConfig)
        {
            bool isReverting = false;

            foreach (var (request, entity) in SystemAPI.Query<RefRW<SwapRequest>>().WithEntityAccess())
            {
                if (request.ValueRO.isReverting)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                RevertSwap(ref state, ref request.ValueRW, gridCells, gridConfig, timingConfig);
                isReverting = true;
            }
            return isReverting;
        }
        
        private void RevertSwap(
            ref SystemState state,
            ref SwapRequest request,
            DynamicBuffer<GridCell> gridCells,
            GridConfig gridConfig,
            TimingConfig timingConfig)
        {
            // Swap tiles back in grid buffer
            var idxA = gridConfig.GetIndex(request.posA);
            var idxB = gridConfig.GetIndex(request.posB);

            gridCells[idxA] = new() { tile = request.tileA };
            gridCells[idxB] = new() { tile = request.tileB };

            // Update tile positions
            var dataA = state.EntityManager.GetComponentData<TileData>(request.tileA);
            var dataB = state.EntityManager.GetComponentData<TileData>(request.tileB);
            dataA.gridPos = request.posA;
            dataB.gridPos = request.posB;
            state.EntityManager.SetComponentData(request.tileA, dataA);
            state.EntityManager.SetComponentData(request.tileB, dataB);

            // Start reverse animation
            TileMoveHelper.Start(state.EntityManager, request.tileA, new(request.posA, 0), timingConfig.swapDuration, TileState.Swap);
            TileMoveHelper.Start(state.EntityManager, request.tileB, new(request.posB, 0), timingConfig.swapDuration, TileState.Swap);

            request.isReverting = true;
        }
    }
}
