using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;
using MiniIT.CONTROLLERS;
using MiniIT.CORE;
using UniRx;

namespace MiniIT.GAME
{
    public class GameStateMachine
    {
        [Inject] private readonly GridController gridController;
        [Inject] private readonly MatchController matchController;
        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly SoundController soundController;
        [Inject] private readonly GameConfig config;

        public readonly ReactiveProperty<GameState> State = null;
        public readonly Subject<Unit> OnGameOver = null;

        private readonly CancellationTokenSource cts = null;
        
        public bool CanInput => State.Value == GameState.Idle;

        public GameStateMachine()
        {
            State = new ReactiveProperty<GameState>(GameState.Idle);
            OnGameOver = new Subject<Unit>();
            cts = new CancellationTokenSource();
        }

        public void Dispose()
        {
            State?.Dispose();
            OnGameOver?.Dispose();
            cts?.Cancel();
            cts?.Dispose();
        }

        public async UniTask ProcessSwapAsync(Cell cellA, Cell cellB)
        {
            if (!CanInput)
            {
                return;
            }

            State.Value = GameState.Swapping;

            await gridController.SwapTilesAsync(cellA, cellB, cts.Token);

            State.Value = GameState.CheckingMatch;

            var matches = matchController.FindMatches();
            if (matches.Count > 0)
            {
                await ProcessMatchesLoop();
            }
            else
            {
                await gridController.SwapTilesAsync(cellB, cellA, cts.Token);
                State.Value = GameState.Idle;
            }
        }

        private async UniTask ProcessMatchesLoop()
        {
            while (true)
            {
                State.Value = GameState.CheckingMatch;
                
                var matches = matchController.FindMatches();
                if (matches.Count == 0)
                {
                    if (!matchController.HasPossibleMoves())
                    {
                        State.Value = GameState.Idle;
                        OnGameOver?.OnNext(Unit.Default);
                        break;
                    }
                    
                    State.Value = GameState.Idle;
                    break;
                }
                
                await UniTask.Delay((int)(config.MatchDelay * 1000), cancellationToken: cts.Token);

                State.Value = GameState.Destroying;
                soundController.PlayMatch();
                
                await matchController.DestroyMatchesAsync(matches);
                scoreController.AddScore(matches.Count * 10);

                State.Value = GameState.Falling;
                await gridController.FallTilesAsync(cts.Token);

                State.Value = GameState.Filling;
                await gridController.FillEmptyCellsAsync(cts.Token);
                matchController.InvalidateHasPossibleMoves();

                soundController.PlayDrop();
            }
        }
    }
}
