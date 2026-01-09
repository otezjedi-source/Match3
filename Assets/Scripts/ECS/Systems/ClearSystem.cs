using System;
using Match3.ECS.Components;
using Match3.Game;
using Unity.Collections;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(MatchSystem))]
    public partial struct ClearSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<MatchConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            if (gameState.ValueRO.Phase != GamePhase.Clear)
                return;

            gameState.ValueRW.PhaseTimer -= SystemAPI.Time.DeltaTime;
            if (gameState.ValueRO.PhaseTimer > 0)
                return;

            var clearQuery = SystemAPI.QueryBuilder().WithAll<ClearTag>().Build();
            if (!clearQuery.IsEmpty)
                return;

            var matchQuery = SystemAPI.QueryBuilder().WithAll<MatchTag>().Build();
            if (matchQuery.IsEmpty)
            {
                gameState.ValueRW.Phase = GamePhase.Fall;
                return;
            }

            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);
            var matchConfig = SystemAPI.GetSingleton<MatchConfig>();

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            int clearCount = 0;

            for (int i = 0; i < gridCells.Length; i++)
            {
                if (gridCells[i].IsEmpty)
                    continue;

                var tile = gridCells[i].Tile;
                if (state.EntityManager.HasComponent<MatchTag>(tile) &&
                    !state.EntityManager.HasComponent<ClearTag>(tile))
                {
                    ecb.AddComponent<ClearTag>(tile);
                    gridCells[i] = new() { Tile = Entity.Null };
                    ++clearCount;
                }
            }

            if (clearCount > 0)
            {
                var scoreEntity = ecb.CreateEntity();
                ecb.AddComponent(scoreEntity, new ScoreEvent { Points = clearCount * matchConfig.PointsPerTile });

                var soundEntity = ecb.CreateEntity();
                ecb.AddComponent(soundEntity, new PlaySoundRequest { Type = SoundType.Match });
            }

            foreach (var (_, entity) in SystemAPI.Query<RefRO<SwapRequest>>().WithEntityAccess())
                ecb.DestroyEntity(entity);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            var dirtyFlag = SystemAPI.GetComponentRW<GridDirtyFlag>(gridEntity);
            dirtyFlag.ValueRW.IsDirty = true;
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

        public readonly void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.TileFactory == null)
                return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (viewData, entity) in SystemAPI.Query<TileViewData>()
                .WithAll<ClearTag, MatchTag>().WithEntityAccess())
            {
                if (viewData.View == null)
                    continue;

                ecb.RemoveComponent<MatchTag>(entity);
                ecb.SetComponent<TileStateData>(entity, new() { State = TileState.Clear });
                RunClearAnimation(viewData.View, entity, refs.TileFactory, state.EntityManager);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        private readonly async void RunClearAnimation(TileView view, Entity entity,
            Factories.TileFactory factory, EntityManager entityMgr)
        {
            try
            {
                await view.ClearAnimationAsync();
                Clear();
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                Clear();
            }

            void Clear()
            {
                if (entityMgr.Exists(entity))
                {
                    if (entityMgr.HasComponent<ClearTag>(entity))
                        entityMgr.RemoveComponent<ClearTag>(entity);
                    factory.Return(entity);
                }
            }
        }
    }
}
