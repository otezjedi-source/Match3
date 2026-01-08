using Match3.ECS.Components;
using Unity.Collections;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ScoreSyncSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
        }

        public readonly void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.ScoreController == null)
                return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (scoreEvent, entity) in SystemAPI.Query<RefRO<ScoreEvent>>().WithEntityAccess())
            {
                refs.ScoreController.AddScore(scoreEvent.ValueRO.Points);
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
