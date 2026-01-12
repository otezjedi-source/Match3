using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(GameSyncSystemGroup))]
    public partial struct ScoreSyncSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.ScoreController == null)
                return;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (scoreEvent, entity) in SystemAPI.Query<RefRO<ScoreEvent>>().WithEntityAccess())
            {
                refs.ScoreController.AddScore(scoreEvent.ValueRO.Points);
                ecb.DestroyEntity(entity);
            }
        }
    }
}
