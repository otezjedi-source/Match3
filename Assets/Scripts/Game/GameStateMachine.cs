using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;
using MiniIT.CONTROLLERS;
using MiniIT.CORE;
using MiniIT.ECS;
using MiniIT.ECS.Components;
using MiniIT.ECS.Systems;
using UniRx;

namespace MiniIT.GAME
{
    public class GameStateMachine
    {
        [Inject] private readonly EcsWorld _world;
        [Inject] private readonly SwapSystem _swapSystem;
        [Inject] private readonly MatchDetectionSystem _matchSystem;
        [Inject] private readonly DestroySystem _destroySystem;
        [Inject] private readonly FallSystem _fallSystem;
        [Inject] private readonly FillSystem _fillSystem;
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

        public async UniTask ProcessSwapAsync(Entity cellEntityA, Entity cellEntityB)
        {
            if (!CanInput)
                return;

            var cellCompA = _world.GetComponent<CellComponent>(cellEntityA);
            var cellCompB = _world.GetComponent<CellComponent>(cellEntityB);

            if (cellCompA.TileEntity.IsNull || cellCompB.TileEntity.IsNull)
                return;

            var tileEntityA = cellCompA.TileEntity;
            var tileEntityB = cellCompB.TileEntity;

            State.Value = GameState.Swapping;

            await _swapSystem.SwapTilesAsync(tileEntityA, tileEntityB, cts.Token);

            State.Value = GameState.CheckingMatch;

            var matches = _matchSystem.FindMatches();
            if (matches.Count > 0)
            {
                await ProcessMatchesLoop();
            }
            else
            {
                await _swapSystem.SwapTilesAsync(tileEntityB, tileEntityA, cts.Token);
                State.Value = GameState.Idle;
            }
        }

        private async UniTask ProcessMatchesLoop()
        {
            while (true)
            {
                State.Value = GameState.CheckingMatch;

                var matches = _matchSystem.FindMatches();
                if (matches.Count == 0)
                {
                    if (!_matchSystem.HasPossibleMoves())
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

                await _destroySystem.DestroyTilesAsync(matches);
                scoreController.AddScore(matches.Count * 10);

                State.Value = GameState.Falling;
                await _fallSystem.FallTilesAsync(cts.Token);

                State.Value = GameState.Filling;
                await _fillSystem.FillEmptyCellsAsync(cts.Token);
                _matchSystem.InvalidateHasPossibleMoves();
            }
        }
    }
}
