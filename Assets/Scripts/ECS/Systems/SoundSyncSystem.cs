using Match3.Controllers;
using Match3.ECS.Components;
using Unity.Collections;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct SoundSyncSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
        }

        public readonly void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.SoundController == null)
                return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, entity) in SystemAPI.Query<RefRO<PlaySoundRequest>>().WithEntityAccess())
            {
                Play(refs.SoundController, request.ValueRO.Type);
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        private readonly void Play(SoundController soundController, SoundType type)
        {
            switch (type)
            {
                case SoundType.Match:
                    soundController.PlayMatch();
                    break;
                case SoundType.Drop:
                    soundController.PlayDrop();
                    break;
                case SoundType.BtnClick:
                    soundController.PlayBtnClick();
                    break;
            }
        }
    }
}