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
        private EntityQuery playerSwapRequestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<TimingConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            playerSwapRequestQuery = SystemAPI.QueryBuilder().WithAll<PlayerSwapRequest>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            if (gameState.ValueRO.Phase != GamePhase.Idle)
            {
                // Discard requests during animations
                if (!playerSwapRequestQuery.IsEmpty)
                    state.EntityManager.DestroyEntity(playerSwapRequestQuery);
                return;
            }

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var timingConfig = SystemAPI.GetSingleton<TimingConfig>();
            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            bool playSound = false;

            foreach (var (request, entity) in SystemAPI.Query<RefRO<PlayerSwapRequest>>().WithEntityAccess())
            {
                var posA = request.ValueRO.PosA;
                var posB = request.ValueRO.PosB;
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
                var tileA = gridCells[idxA].Tile;
                var tileB = gridCells[idxB].Tile;
                gridCells[idxA] = new() { Tile = tileB };
                gridCells[idxB] = new() { Tile = tileA };

                // Update tile grid positions
                var tileLookup = SystemAPI.GetComponentLookup<TileData>(true);
                var tileDataA = tileLookup[tileA];
                var tileDataB = tileLookup[tileB];
                tileDataA.GridPos = posB;
                tileDataB.GridPos = posA;
                state.EntityManager.SetComponentData(tileA, tileDataA);
                state.EntityManager.SetComponentData(tileB, tileDataB);

                // Start swap animations
                TileMoveHelper.Start(state.EntityManager, tileA, new(posB, 0), timingConfig.SwapDuration, TileState.Swap);
                TileMoveHelper.Start(state.EntityManager, tileB, new(posA, 0), timingConfig.SwapDuration, TileState.Swap);
                playSound = true;

                // Track swap for potential revert if no match
                var swapRequest = ecb.CreateEntity();
                ecb.AddComponent(swapRequest, new SwapRequest
                {
                    TileA = tileA,
                    TileB = tileB,
                    PosA = posA,
                    PosB = posB,
                    IsReverting = false,
                });

                gameState.ValueRW.Phase = GamePhase.Swap;

                var dirtyFlag = SystemAPI.GetComponentRW<GridDirtyFlag>(gridEntity);
                dirtyFlag.ValueRW.IsDirty = true;

                ecb.DestroyEntity(entity);
            }

            if (playSound)
            {
                var soundEntity = ecb.CreateEntity();
                ecb.AddComponent(soundEntity, new PlaySoundRequest { Type = SoundType.Swap });
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
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            if (gameState.ValueRO.Phase != GamePhase.Swap)
                return;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, entity) in SystemAPI.Query<RefRW<SwapRequest>>().WithEntityAccess())
            {
                var tileA = request.ValueRO.TileA;
                var tileB = request.ValueRO.TileB;

                // Check if tiles are still moving
                bool movingA = state.EntityManager.IsComponentEnabled<TileMove>(tileA);
                bool movingB = state.EntityManager.IsComponentEnabled<TileMove>(tileB);
                if (movingA || movingB)
                    continue;

                if (request.ValueRO.IsReverting)
                {
                    // Revert complete, back to idle
                    gameState.ValueRW.Phase = GamePhase.Idle;
                    ecb.DestroyEntity(entity);
                }
                else
                    // Swap complete, check for matches
                    gameState.ValueRW.Phase = GamePhase.Match;
            }
        }
    }
}
