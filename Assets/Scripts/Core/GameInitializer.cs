using System;
using MiniIT.CONTROLLERS;
using MiniIT.GAME;
using VContainer;
using VContainer.Unity;

namespace MiniIT.CORE
{
    public class GameInitializer : IStartable, ITickable, IDisposable
    {
        [Inject] private readonly GridController gridController;
        [Inject] private readonly InputController inputController;
        [Inject] private readonly GameStateMachine stateMachine;

        public void Start()
        {
            gridController.Init();
            inputController.Init();
        }

        public void Tick()
        {
            inputController.Update();
        }

        public void Dispose()
        {
            stateMachine.Dispose();
        }
    }
}
