using Match3.ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Processes player swap requests. Validates and executes tile swaps.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(TileMoveSystem))]
    public partial struct SwapSystem : ISystem
    {
        private ComponentLookup<TileData> tileLookup;
        private EntityQuery playerSwapRequestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<TimingConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            tileLookup = SystemAPI.GetComponentLookup<TileData>(true);
            playerSwapRequestQuery = SystemAPI.QueryBuilder().WithAll<PlayerSwapRequest>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            if (gameState.ValueRO.phase != GamePhase.Idle)
            {
                // Discard requests during animations
                if (!playerSwapRequestQuery.IsEmpty)
                    state.EntityManager.DestroyEntity(playerSwapRequestQuery);
                return;
            }

            if (playerSwapRequestQuery.IsEmpty)
                return;
            
            tileLookup.Update(ref state);

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var timingConfig = SystemAPI.GetSingleton<TimingConfig>();
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            bool playSound = false;

            foreach (var (request, entity) in SystemAPI.Query<RefRO<PlayerSwapRequest>>().WithEntityAccess())
            {
                var posA = request.ValueRO.posA;
                var posB = request.ValueRO.posB;
                if (!gridConfig.IsValidPos(posA) || !gridConfig.IsValidPos(posB))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var idxA = gridConfig.GetIndex(posA);
                var idxB = gridConfig.GetIndex(posB);
                if (gridCells[idxA].IsEmpty || gridCells[idxB].IsEmpty)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Swap tiles in grid
                var tileA = gridCells[idxA].tile;
                var tileB = gridCells[idxB].tile;
                gridCells[idxA] = new() { tile = tileB };
                gridCells[idxB] = new() { tile = tileA };

                // Update tile grid positions
                var tileDataA = tileLookup[tileA];
                var tileDataB = tileLookup[tileB];
                tileDataA.gridPos = posB;
                tileDataB.gridPos = posA;
                state.EntityManager.SetComponentData(tileA, tileDataA);
                state.EntityManager.SetComponentData(tileB, tileDataB);

                // Start swap animations
                TileMoveHelper.Start(state.EntityManager, tileA, new(posB, 0), timingConfig.swapDuration, TileState.Swap);
                TileMoveHelper.Start(state.EntityManager, tileB, new(posA, 0), timingConfig.swapDuration, TileState.Swap);
                playSound = true;

                // Track swap for potential revert if no match
                var swapRequest = ecb.CreateEntity();
                ecb.AddComponent(swapRequest, new SwapRequest
                {
                    tileA = tileA,
                    tileB = tileB,
                    posA = posA,
                    posB = posB,
                    isReverting = false,
                    isHorizontal = posA.y == posB.y,
                });

                gameState.ValueRW.phase = GamePhase.Swap;
                SystemAPI.GetSingletonRW<GridDirtyFlag>().ValueRW.isDirty = true;

                ecb.DestroyEntity(entity);
            }

            if (playSound)
            {
                var soundEntity = ecb.CreateEntity();
                ecb.AddComponent(soundEntity, new PlaySoundRequest { type = SoundType.Swap });
            }
        }
    }

    /// <summary>
    /// Waits for swap animation to complete, then transitions to Match phase.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(SwapSystem))]
    public partial struct SwapCompleteSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            if (gameState.ValueRO.phase != GamePhase.Swap)
                return;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, entity) in SystemAPI.Query<RefRW<SwapRequest>>().WithEntityAccess())
            {
                var tileA = request.ValueRO.tileA;
                var tileB = request.ValueRO.tileB;

                // Check if tiles are still moving
                bool movingA = state.EntityManager.IsComponentEnabled<TileMove>(tileA);
                bool movingB = state.EntityManager.IsComponentEnabled<TileMove>(tileB);
                if (movingA || movingB)
                    continue;

                if (request.ValueRO.isReverting)
                {
                    // Revert complete, back to idle
                    gameState.ValueRW.phase = GamePhase.Idle;
                    ecb.DestroyEntity(entity);
                }
                else
                    // Swap complete, check for matches
                    gameState.ValueRW.phase = GamePhase.Match;
            }
        }
    }
}
