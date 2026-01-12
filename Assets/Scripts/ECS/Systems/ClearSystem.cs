using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(MatchSystem))]
    public partial struct ClearSystem : ISystem
    {
        private EntityQuery clearQuery;
        private EntityQuery matchQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<MatchConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            clearQuery = SystemAPI.QueryBuilder().WithAll<ClearTag>().Build();
            matchQuery = SystemAPI.QueryBuilder().WithAll<MatchTag>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            if (gameState.ValueRO.Phase != GamePhase.Clear)
                return;

            gameState.ValueRW.PhaseTimer -= SystemAPI.Time.DeltaTime;
            if (gameState.ValueRO.PhaseTimer > 0)
                return;

            if (!clearQuery.IsEmpty)
                return;

            if (matchQuery.IsEmpty)
            {
                gameState.ValueRW.Phase = GamePhase.Fall;
                return;
            }

            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);
            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var matchConfig = SystemAPI.GetSingleton<MatchConfig>();

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            int clearCount = 0;

            foreach (var (tileData, viewData, entity) in 
                SystemAPI.Query<RefRO<TileData>, TileViewData>()
                    .WithAll<MatchTag>()
                    .WithNone<ClearTag>()
                    .WithEntityAccess())
            {
                if (viewData.View != null)
                    viewData.View.PlayClearAnim();

                var idx = gridConfig.GetIndex(tileData.ValueRO.GridPos);
                gridCells[idx] = new() { Tile = Entity.Null };
                ecb.AddComponent<ClearTag>(entity);
                ++clearCount;
            }

            if (clearCount > 0)
            {
                var scoreEntity = ecb.CreateEntity();
                ecb.AddComponent(scoreEntity, new ScoreEvent { Points = clearCount * matchConfig.PointsPerTile });

                var soundEntity = ecb.CreateEntity();
                ecb.AddComponent(soundEntity, new PlaySoundRequest { Type = SoundType.Match });

                var dirtyFlag = SystemAPI.GetComponentRW<GridDirtyFlag>(gridEntity);
                dirtyFlag.ValueRW.IsDirty = true;
            }

            foreach (var (_, entity) in SystemAPI.Query<RefRO<SwapRequest>>().WithEntityAccess())
                ecb.DestroyEntity(entity);
        }
    }

    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(ClearSystem))]
    public partial struct ClearCompleteSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.TileFactory == null)
                return;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, entity) in SystemAPI.Query<ClearDoneEvent>()
                .WithAll<ClearTag, MatchTag>().WithEntityAccess())
            {
                ecb.SetComponent<TileStateData>(entity, new() { State = TileState.Clear });
                ecb.RemoveComponent<ClearTag>(entity);
                ecb.RemoveComponent<ClearDoneEvent>(entity);
                ecb.RemoveComponent<DropTag>(entity);
                ecb.RemoveComponent<DropDoneEvent>(entity);
                ecb.RemoveComponent<MatchTag>(entity);
                refs.TileFactory.Return(entity);
            }
        }
    }
}
