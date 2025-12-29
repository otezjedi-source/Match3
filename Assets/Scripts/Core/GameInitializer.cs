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
        [Inject] private readonly MatchController matchController;

        public void Start()
        {
            gridController.Init();
            inputController.Init();

            int i = 0;
            while (!matchController.HasPossibleMoves() && ++i < 100)
                gridController.ResetTiles();
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
