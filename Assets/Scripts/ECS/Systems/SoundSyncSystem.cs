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
        private EntityQuery soundRequestQuery;
        private EntityQuery bonusRequestQuery;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            soundRequestQuery = SystemAPI.QueryBuilder().WithAll<PlaySoundRequest>().Build();
            bonusRequestQuery = SystemAPI.QueryBuilder().WithAll<PlayBonusSoundRequest>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (soundRequestQuery.IsEmpty && bonusRequestQuery.IsEmpty)
                return;
            
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs.soundController == null)
                return;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var request in SystemAPI.Query<RefRO<PlaySoundRequest>>())
                refs.soundController.Play(request.ValueRO.type);
            foreach (var request in SystemAPI.Query<RefRO<PlayBonusSoundRequest>>())
                refs.soundController.PlayBonus(request.ValueRO.type);
            
            ecb.DestroyEntity(soundRequestQuery, EntityQueryCaptureMode.AtPlayback);
            ecb.DestroyEntity(bonusRequestQuery, EntityQueryCaptureMode.AtPlayback);
        }
    }
}