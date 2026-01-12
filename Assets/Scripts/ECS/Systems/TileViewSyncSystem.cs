using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(GameSyncSystemGroup))]
    public partial struct TileViewSyncSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
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
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
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

            if (playSound && refs?.SoundController != null)
                refs.SoundController.PlayDrop();
        }

        private readonly void CompleteDropAnims(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, entity) in
                SystemAPI.Query<RefRO<DropDoneEvent>>().WithAll<DropTag>().WithEntityAccess())
            {
                state.EntityManager.SetComponentData(entity, new TileStateData { State = TileState.Idle });

                ecb.RemoveComponent<DropTag>(entity);
                ecb.RemoveComponent<DropDoneEvent>(entity);
            }
        }
    }
}
