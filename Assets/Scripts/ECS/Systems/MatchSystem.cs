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
        private NativeHashMap<int, MatchGroup> groupsCache;
        private NativeList<Entity> markTiles;
        private ComponentLookup<MatchTag> matchLookup;

        private struct SwapInfo
        {
            public int2 posA;
            public int2 posB;
        }

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
            groupsCache = new(8, Allocator.Persistent);
            markTiles = new(32, Allocator.Persistent);
            matchLookup = SystemAPI.GetComponentLookup<MatchTag>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (matchesCache.IsCreated)
                matchesCache.Dispose();
            if (groupsCache.IsCreated)
                groupsCache.Dispose();
            if (markTiles.IsCreated)
                markTiles.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            if (gameState.ValueRO.phase != GamePhase.Match)
                return;
            
            matchLookup.Update(ref state);

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var matchConfig = SystemAPI.GetSingleton<MatchConfig>();
            var typeCache = SystemAPI.GetSingletonBuffer<GridTileTypeCache>();

            FindMatches(typeCache, gridConfig, matchConfig);
            
            var matchGroups = SystemAPI.GetSingletonBuffer<MatchGroup>();
            var swap = GetSwapInfo(ref state);

            BuildMatchGroups(typeCache, matchGroups, gridConfig, swap);

            if (matchGroups.Length > 0)
                ProcessMatches(ref state, gridConfig);
            else
                ProcessNoMatches(ref state, gridConfig);
        }

        /// <summary>
        /// Find all matches using line scanning algorithm
        /// </summary>
        private void FindMatches(
            DynamicBuffer<GridTileTypeCache> typeCache,
            GridConfig gridConfig,
            MatchConfig matchConfig)
        {
            matchesCache.Clear();
            if (matchesCache.Capacity < gridConfig.CellCount)
                matchesCache.Capacity = gridConfig.CellCount;

            ScanLines(matchesCache, typeCache, gridConfig, matchConfig, true);
            ScanLines(matchesCache, typeCache, gridConfig, matchConfig, false);
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

        /// <summary>
        /// Get swap info of exists
        /// </summary>
        private SwapInfo? GetSwapInfo(ref SystemState state)
        {
            foreach (var request in SystemAPI.Query<RefRO<SwapRequest>>())
            {
                if (request.ValueRO.isReverting)
                    continue;

                return new()
                {
                    posA = request.ValueRO.posA,
                    posB = request.ValueRO.posB,
                };
            }

            return null;
        }
        
        [BurstCompile]
        private void BuildMatchGroups(
            DynamicBuffer<GridTileTypeCache> typeCache,
            DynamicBuffer<MatchGroup> matchGroups, 
            GridConfig gridConfig, 
            SwapInfo? swap)
        {
            matchGroups.Clear();
            groupsCache.Clear();

            foreach (var pos in matchesCache)
            {
                int idx = gridConfig.GetIndex(pos);
                var type = typeCache[idx].type;
                if (type == TileType.None)
                    continue;

                int key = (int)type;

                if (!groupsCache.TryGetValue(key, out var group))
                {
                    group = new MatchGroup
                    {
                        type = type,
                        minPos = pos,
                        maxPos = pos,
                        bonusPos = pos
                    };
                }

                group.count++;
                group.minPos = math.min(group.minPos, pos);
                group.maxPos = math.max(group.maxPos, pos);

                if (swap.HasValue)
                {
                    if (pos.Equals(swap.Value.posB))
                        group.bonusPos = swap.Value.posB;
                    else if (pos.Equals(swap.Value.posA) && !group.bonusPos.Equals(swap.Value.posB))
                        group.bonusPos = swap.Value.posA;
                }
                else
                    group.bonusPos = (group.minPos + group.maxPos) / 2;

                groupsCache[key] = group;
            }

            foreach (var kvp in groupsCache)
                matchGroups.Add(kvp.Value);
        }
        
        private void ProcessMatches(ref SystemState state, GridConfig gridConfig)
        {
            var timingConfig = SystemAPI.GetSingleton<TimingConfig>();
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();
            
            MarkMatchedTiles(ref state, gridCells, gridConfig);

            var gameState = SystemAPI.GetSingletonRW<GameState>();
            gameState.ValueRW.phase = GamePhase.Clear;
            gameState.ValueRW.phaseTimer = timingConfig.matchDelay;
        }
        
        private void MarkMatchedTiles(
            ref SystemState state,
            DynamicBuffer<GridCell> gridCells,
            GridConfig gridConfig)
        {
            markTiles.Clear();
            
            foreach (var pos in matchesCache)
            {
                int idx = gridConfig.GetIndex(pos);
                var tile = gridCells[idx].tile;
                if (tile != Entity.Null && !matchLookup.HasComponent(tile))
                    markTiles.Add(tile);
            }

            state.EntityManager.AddComponent<MatchTag>(markTiles.AsArray());
        }

        private void ProcessNoMatches(ref SystemState state, GridConfig gridConfig)
        {
            var timingConfig = SystemAPI.GetSingleton<TimingConfig>();
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();
            
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            bool isReverting = TryRevertSwaps(ref state, ecb, gridCells, gridConfig, timingConfig);
            if (isReverting)
                SystemAPI.GetSingletonRW<GridDirtyFlag>().ValueRW.isDirty = true;
            
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            gameState.ValueRW.phase = isReverting ? GamePhase.Swap : GamePhase.Idle;
        }

        /// <summary>
        /// If no matches were found after a swap, revert the swap animation.
        /// </summary>
        private bool TryRevertSwaps(
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
