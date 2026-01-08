using Match3.ECS.Components;
using Unity.Collections;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct TileViewSyncSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
        }

        public readonly void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            bool playDrop = false;

            foreach (var (worldPos, viewData) in
                SystemAPI.Query<RefRO<TileWorldPos>, TileViewData>()
                    .WithAll<TileMove>())
            {
                if (viewData.View != null)
                    viewData.View.transform.position = worldPos.ValueRO.Pos;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (worldPos, stateData, viewData, entity) in
                SystemAPI.Query<RefRO<TileWorldPos>, RefRO<TileStateData>, TileViewData>()
                    .WithDisabled<TileMove>()
                    .WithNone<DropTag>()
                    .WithEntityAccess())
            {
                if (viewData.View == null)
                    continue;

                viewData.View.transform.position = worldPos.ValueRO.Pos;

                if (stateData.ValueRO.State != TileState.Fall)
                    continue;

                ecb.AddComponent<DropTag>(entity);
                RunDropAnimation(viewData.View, entity, state.EntityManager);
                playDrop = true;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            if (playDrop && refs?.SoundController != null)
                refs.SoundController.PlayDrop();
        }

        private static async void RunDropAnimation(Game.TileView view, Entity entity, EntityManager em)
        {
            try
            {
                await view.DropAnimationAsync();
            }
            finally
            {
                if (em.Exists(entity))
                {
                    if (em.HasComponent<DropTag>(entity))
                        em.RemoveComponent<DropTag>(entity);
                    em.SetComponentData(entity, new TileStateData { State = TileState.Idle });
                }
            }
        }
    }
}
