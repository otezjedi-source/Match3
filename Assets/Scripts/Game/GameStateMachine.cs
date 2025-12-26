using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;
using Match3.Controllers;
using Match3.Core;
using UniRx;
using System.Collections.Generic;
using System;
using Unity.Mathematics;

namespace Match3.Game
{
    public class GameStateMachine
    {
        enum State
        {
            Idle = 1,
            Swapping = 2,
            Matching = 3,
            Destroying = 4,
            Falling = 5,
            Filling = 6,
        }

        [Inject] private readonly GridController gridController;
        [Inject] private readonly MatchController matchController;
        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly SoundController soundController;
        [Inject] private readonly GameConfig config;

        public readonly Subject<Unit> OnGameOver = new();

        private State state = State.Idle;        
        public bool CanInput => state == State.Idle;

        public void Dispose()
        {
            OnGameOver?.Dispose();
        }

        public async UniTask ProcessSwapAsync(int2 posA, int2 posB, CancellationToken ct = default)
        {
            if (!CanInput)
                return;

            state = State.Swapping;

            await gridController.SwapAsync(posA, posB, ct);

            state = State.Matching;

            var matches = matchController.FindMatches();
            if (matches.Count == 0)
            {
                await gridController.SwapAsync(posB, posA, ct);
                state = State.Idle;
                return;
            }

            await ProcessMatchesLoop(matches);

            state = State.Idle;

            if (!matchController.HasPossibleMoves())    
                OnGameOver?.OnNext(Unit.Default);
        }

        private async UniTask ProcessMatchesLoop(List<int2> matches, CancellationToken ct = default)
        {
            while (matches.Count > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(config.MatchDelay), cancellationToken: ct);

                state = State.Destroying;
                soundController.PlayMatch();
                
                await gridController.RemoveTilesAsync(matches);
                scoreController.AddScore(matches.Count * 10);

                state = State.Falling;
                await gridController.FallTilesAsync(ct);

                state = State.Filling;
                await gridController.FillEmptyCellsAsync(ct);

                matchController.InvalidateHasPossibleMoves();

                state = State.Matching;
                matches = matchController.FindMatches();
            }
        }
    }
}
