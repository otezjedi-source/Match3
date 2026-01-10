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
            SyncMovingTiles(ref state);
            StartDropAnims(ref state, refs);
            CompleteDropAnims(ref state);
        }

        private readonly void SyncMovingTiles(ref SystemState state)
        {
            foreach (var (worldPos, viewData) in
                SystemAPI.Query<RefRO<TileWorldPos>, TileViewData>().WithAll<TileMove>())
            {
                if (viewData.View != null)
                    viewData.View.transform.position = worldPos.ValueRO.Pos;
            }

        }

        private readonly void StartDropAnims(ref SystemState state, ManagedReferences refs)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            bool playSound = false;

            foreach (var (worldPos, stateData, viewData, entity) in
                SystemAPI.Query<RefRO<TileWorldPos>, RefRO<TileStateData>, TileViewData>()
                    .WithDisabled<TileMove>()
                    .WithNone<DropTag, DropDoneEvent>()
                    .WithEntityAccess())
            {
                if (viewData.View == null)
                    continue;

                viewData.View.transform.position = worldPos.ValueRO.Pos;

                if (stateData.ValueRO.State != TileState.Fall)
                    continue;

                viewData.View.PlayDropAnim();
                ecb.AddComponent<DropTag>(entity);
                playSound = true;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            if (playSound && refs?.SoundController != null)
                refs.SoundController.PlayDrop();
        }

        private readonly void CompleteDropAnims(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, entity) in
                SystemAPI.Query<RefRO<DropDoneEvent>>().WithAll<DropTag>().WithEntityAccess())
            {
                state.EntityManager.SetComponentData(entity, new TileStateData { State = TileState.Idle });

                ecb.RemoveComponent<DropTag>(entity);
                ecb.RemoveComponent<DropDoneEvent>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
