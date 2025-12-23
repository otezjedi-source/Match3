using System;
using MiniIT.ECS;
using MiniIT.ECS.Systems;
using MiniIT.GAME;
using VContainer;
using VContainer.Unity;

namespace MiniIT.CORE
{
    public class GameInitializer : IStartable, ITickable, IDisposable
    {
        [Inject] private readonly EcsWorld _world;
        [Inject] private readonly GridInitializationSystem _gridSystem;
        [Inject] private readonly InputSystem _inputSystem;
        [Inject] private readonly GameStateMachine _stateMachine;

        public void Start()
        {
            _gridSystem.Initialize();
            _inputSystem.Init();
        }

        public void Tick()
        {
            _inputSystem.Update();
        }

        public void Dispose()
        {
            _gridSystem.Cleanup();
            _world.Clear();
            _stateMachine.Dispose();
        }
    }
}
