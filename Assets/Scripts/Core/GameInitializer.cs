using System;
using Match3.Controllers;
using Match3.Game;
using VContainer;
using VContainer.Unity;

namespace Match3.Core
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
