using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;
using MiniIT.CONTROLLERS;
using MiniIT.CORE;
using UnityEngine.Events;

namespace MiniIT.GAME
{
    public class GameStateMachine
    {
        [Inject] private readonly GridController gridController;
        [Inject] private readonly MatchController matchController;
        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly SoundController soundController;
        [Inject] private readonly GameConfig config;

        public UnityEvent OnGameOver = null;
        private readonly CancellationTokenSource cts = null;

        public GameState State { get; private set; } = GameState.Idle;
        public bool CanInput => State == GameState.Idle;

        public GameStateMachine()
        {
            OnGameOver = new UnityEvent();
            cts = new CancellationTokenSource();
        }

        public void Dispose()
        {
            cts?.Cancel();
            cts?.Dispose();
        }

        public async UniTask ProcessSwapAsync(Cell cellA, Cell cellB)
        {
            if (!CanInput)
            {
                return;
            }

            State = GameState.Swapping;

            await gridController.SwapTilesAsync(cellA, cellB, cts.Token);

            State = GameState.CheckingMatch;
            var matches = matchController.FindMatches();

            if (matches.Count > 0)
            {
                await ProcessMatchesLoop();
            }
            else
            {
                await gridController.SwapTilesAsync(cellB, cellA, cts.Token);
                State = GameState.Idle;
            }
        }

        private async UniTask ProcessMatchesLoop()
        {
            while (true)
            {
                State = GameState.CheckingMatch;
                var matches = matchController.FindMatches();

                if (matches.Count == 0)
                {
                    if (!matchController.HasPossibleMoves())
                    {
                        State = GameState.Idle;
                        OnGameOver?.Invoke();
                        break;
                    }
                    
                    State = GameState.Idle;
                    break;
                }
                
                await UniTask.Delay((int)(config.MatchDelay * 1000), cancellationToken: cts.Token);

                State = GameState.Destroying;
                soundController.PlayMatch();
                await matchController.DestroyMatchesAsync(matches);
                scoreController.AddScore(matches.Count * 10);

                State = GameState.Falling;
                soundController.PlayDrop();
                await gridController.FallTilesAsync(cts.Token);

                State = GameState.Filling;
                soundController.PlayDrop();
                await gridController.FillEmptyCellsAsync(cts.Token);
            }
        }
    }
}
