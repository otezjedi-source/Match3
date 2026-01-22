using Match3.Controllers;
using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Processes PlaySoundRequest entities and triggers audio.
    /// </summary>
    [UpdateInGroup(typeof(GameSyncSystemGroup))]
    public partial struct SoundSyncSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.SoundController == null)
                return;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, entity) in SystemAPI.Query<RefRO<PlaySoundRequest>>().WithEntityAccess())
            {
                refs.SoundController.Play(request.ValueRO.Type);
                ecb.DestroyEntity(entity);
            }

            foreach (var (request, entity) in SystemAPI.Query<RefRO<PlayBonusSoundRequest>>().WithEntityAccess())
            {
                refs.SoundController.PlayBonus(request.ValueRO.Type);
                ecb.DestroyEntity(entity);
            }
        }
    }
}