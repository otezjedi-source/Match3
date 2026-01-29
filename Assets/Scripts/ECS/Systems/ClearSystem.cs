using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Starts clear animations for matched tiles. Awards score.
    /// </summary>
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(MatchSystem))]
    public partial struct ClearSystem : ISystem
    {
        private EntityQuery clearQuery;
        private EntityQuery matchQuery;
        private EntityQuery swapRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<MatchConfig>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            clearQuery = SystemAPI.QueryBuilder().WithAll<ClearTag>().Build();
            matchQuery = SystemAPI.QueryBuilder().WithAll<MatchTag>().Build();
            swapRequestQuery = SystemAPI.QueryBuilder().WithAll<SwapRequest>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            if (gameState.ValueRO.phase != GamePhase.Clear)
                return;

            // Wait for delay timer
            gameState.ValueRW.phaseTimer -= SystemAPI.Time.DeltaTime;
            if (gameState.ValueRO.phaseTimer > 0)
                return;

            // Wait for clear animations to finish
            if (!clearQuery.IsEmpty)
                return;

            // No more matches - start falling
            if (matchQuery.IsEmpty)
            {
                gameState.ValueRW.phase = GamePhase.Fall;
                return;
            }
            
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();
            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var matchConfig = SystemAPI.GetSingleton<MatchConfig>();
            
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().
                CreateCommandBuffer(state.WorldUnmanaged);

            int clearCount = 0;

            // Start clear animation for each matched tile
            foreach (var (tileData, viewData, entity) in
                SystemAPI.Query<RefRO<TileData>, TileViewData>()
                    .WithAll<MatchTag>()
                    .WithNone<ClearTag>()
                    .WithEntityAccess())
            {
                viewData.view?.PlayClearAnim();

                // Remove tile from grid
                var idx = gridConfig.GetIndex(tileData.ValueRO.gridPos);
                gridCells[idx] = new() { tile = Entity.Null };
                ecb.AddComponent<ClearTag>(entity);
                ++clearCount;
            }

            if (clearCount > 0)
            {
                // Award score
                var scoreEntity = ecb.CreateEntity();
                ecb.AddComponent<ScoreEvent>(scoreEntity, new() { points = clearCount * matchConfig.pointsPerTile });

                // Play sound
                var soundEntity = ecb.CreateEntity();
                ecb.AddComponent<PlaySoundRequest>(soundEntity, new() { type = SoundType.Match });
                
                SystemAPI.GetSingletonRW<GridDirtyFlag>().ValueRW.isDirty = true;
            }

            // Cleanup swap request
            ecb.DestroyEntity(swapRequestQuery, EntityQueryCaptureMode.AtPlayback);
        }
    }

    /// <summary>
    /// Returns cleared tiles to pool after animation completes.
    /// </summary>
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(ClearSystem))]
    public partial struct ClearCompleteSystem : ISystem
    {
        private EntityQuery clearQuery;
        private ComponentTypeSet removeSet;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            clearQuery = SystemAPI.QueryBuilder()
                .WithAll<ClearDoneEvent, ClearTag, MatchTag, TileStateData>()
                .Build();
            
            state.RequireForUpdate(clearQuery);
            
            removeSet = new ComponentTypeSet(
                ComponentType.ReadWrite<ClearTag>(),
                ComponentType.ReadWrite<ClearDoneEvent>(),
                ComponentType.ReadWrite<DropTag>(),
                ComponentType.ReadWrite<DropDoneEvent>(),
                ComponentType.ReadWrite<MatchTag>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs.tileFactory == null)
                return;
            
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            ecb.RemoveComponent(clearQuery, removeSet, EntityQueryCaptureMode.AtPlayback);
            
            foreach (var (stateData, entity) in SystemAPI.Query<RefRW<TileStateData>>()
                         .WithAll<ClearDoneEvent, ClearTag, MatchTag>()
                         .WithEntityAccess())
            {
                stateData.ValueRW.state = TileState.Clear;
                refs.tileFactory.Return(entity);
            }
        }
    }
}
