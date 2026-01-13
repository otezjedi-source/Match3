using Match3.ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(SwapCompleteSystem))]
    public partial struct MatchSystem : ISystem
    {
        private NativeHashSet<int2> matchesCache;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<MatchConfig>();
            state.RequireForUpdate<TimingConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            matchesCache = new(64, Allocator.Persistent);
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
            if (gameState.ValueRO.Phase != GamePhase.Match)
                return;

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var matchConfig = SystemAPI.GetSingleton<MatchConfig>();
            var timingConfig = SystemAPI.GetSingleton<TimingConfig>();
            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            var typeCache = SystemAPI.GetBuffer<GridTileTypeCache>(gridEntity);
            var matchResults = SystemAPI.GetBuffer<MatchResult>(gridEntity);

            matchesCache.Clear();
            if (matchesCache.Capacity < gridConfig.CellCount)
                matchesCache.Capacity = gridConfig.CellCount;

            ScanLines(matchesCache, typeCache, gridConfig, matchConfig, true);
            ScanLines(matchesCache, typeCache, gridConfig, matchConfig, false);

            matchResults.Clear();
            foreach (var pos in matchesCache)
                matchResults.Add(new() { Pos = pos });

            if (matchResults.Length > 0)
            {
                MarkMatchedTiles(ref state, matchResults, gridConfig, gridEntity);

                gameState = SystemAPI.GetSingletonRW<GameState>();
                gameState.ValueRW.Phase = GamePhase.Clear;
                gameState.ValueRW.PhaseTimer = timingConfig.MatchDelay;
                return;
            }

            bool isReverting = HandleSwaps(ref state, gridEntity, gridConfig, timingConfig);
            if (isReverting)
            {
                var dirtyFlag = SystemAPI.GetSingletonRW<GridDirtyFlag>();
                dirtyFlag.ValueRW.IsDirty = true;
            }
            gameState.ValueRW.Phase = isReverting ? GamePhase.Swap : GamePhase.Idle;
        }

        [BurstCompile]
        private readonly void ScanLines(
            NativeHashSet<int2> matches,
            DynamicBuffer<GridTileTypeCache> typeCache,
            GridConfig gridConfig, MatchConfig matchConfig,
            bool horizontal)
        {
            int linesCount = horizontal ? gridConfig.Height : gridConfig.Width;
            int lineLength = horizontal ? gridConfig.Width : gridConfig.Height;

            for (int lineIdx = 0; lineIdx < linesCount; lineIdx++)
            {
                int i = 0;
                while (i < lineLength)
                {
                    int x = horizontal ? i : lineIdx;
                    int y = horizontal ? lineIdx : i;
                    int idx = gridConfig.GetIndex(x, y);

                    var type = typeCache[idx].Type;
                    if (type == TileType.None)
                    {
                        i++;
                        continue;
                    }

                    int j = i + 1;
                    while (j < lineLength)
                    {
                        int nx = horizontal ? j : lineIdx;
                        int ny = horizontal ? lineIdx : j;
                        int nidx = gridConfig.GetIndex(nx, ny);

                        if (typeCache[nidx].Type != type)
                            break;

                        j++;
                    }

                    if (j - i >= matchConfig.MatchCount)
                    {
                        for (int k = i; k < j; k++)
                            matches.Add(horizontal ? new(k, lineIdx) : new(lineIdx, k));
                    }

                    i = j;
                }
            }
        }

        private void MarkMatchedTiles(
            ref SystemState state,
            DynamicBuffer<MatchResult> matches,
            GridConfig gridConfig,
            Entity gridEntity)
        {
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);
            var matchLookup = SystemAPI.GetComponentLookup<MatchTag>(true);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var match in matches)
            {
                int idx = gridConfig.GetIndex(match.Pos);
                var tile = gridCells[idx].Tile;
                if (tile != Entity.Null && !matchLookup.HasComponent(tile))
                    ecb.AddComponent<MatchTag>(tile);
            }
        }

        private bool HandleSwaps(
            ref SystemState state,
            Entity gridEntity,
            GridConfig gridConfig,
            TimingConfig timingConfig)
        {
            bool isReverting = false;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, entity) in SystemAPI.Query<RefRW<SwapRequest>>().WithEntityAccess())
            {
                if (request.ValueRO.IsReverting)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }
                
                RevertSwap(ref state, ref request.ValueRW, gridEntity, gridConfig, timingConfig);
                isReverting = true;   
            }
            return isReverting;
        }
        
        private void RevertSwap(
            ref SystemState state,
            ref SwapRequest request,
            Entity gridEntity,
            GridConfig gridConfig,
            TimingConfig timingConfig)
        {
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);

            var idxA = gridConfig.GetIndex(request.PosA);
            var idxB = gridConfig.GetIndex(request.PosB);

            gridCells[idxA] = new() { Tile = request.TileA };
            gridCells[idxB] = new() { Tile = request.TileB };

            var dataA = state.EntityManager.GetComponentData<TileData>(request.TileA);
            var dataB = state.EntityManager.GetComponentData<TileData>(request.TileB);
            dataA.GridPos = request.PosA;
            dataB.GridPos = request.PosB;
            state.EntityManager.SetComponentData(request.TileA, dataA);
            state.EntityManager.SetComponentData(request.TileB, dataB);

            TileMoveHelper.Start(state.EntityManager, request.TileA, new(request.PosA, 0), timingConfig.SwapDuration, TileState.Swap);
            TileMoveHelper.Start(state.EntityManager, request.TileB, new(request.PosB, 0), timingConfig.SwapDuration, TileState.Swap);

            request.IsReverting = true;
        }
    }
}
