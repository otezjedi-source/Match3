using Match3.Controllers;
using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
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
                Play(refs.SoundController, request.ValueRO.Type);
                ecb.DestroyEntity(entity);
            }
        }
        
        private readonly void Play(SoundController soundController, SoundType type)
        {
            switch (type)
            {
                case SoundType.Swap:
                    soundController.PlaySwap();
                    break;
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