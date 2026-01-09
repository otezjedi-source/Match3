using System;
using Match3.ECS.Components;
using UniRx;
using Unity.Entities;
using VContainer;

namespace Match3.Controllers
{
    public class GameController : IDisposable
    {
        [Inject] private readonly EntityManager entityMgr;
        [Inject] private readonly ScoreController scoreController;

        private EntityQuery gameStateQuery;
        private readonly CompositeDisposable disposables = new();

        public readonly ReactiveProperty<GamePhase> CurrentPhase = new(GamePhase.Idle);
        public readonly ReactiveProperty<bool> IsGameOver = new(false);

        public void Init()
        {
            gameStateQuery = entityMgr.CreateEntityQuery(typeof(GameState));

            Observable.EveryUpdate()
                .Subscribe(_ => UpdateGameState())
                .AddTo(disposables);
        }

        private void UpdateGameState()
        {
            if (gameStateQuery.IsEmpty)
                return;

            var gameState = gameStateQuery.GetSingleton<GameState>();
            CurrentPhase.Value = gameState.Phase;

            var gameOverQuery = entityMgr.CreateEntityQuery(typeof(GameOverEvent));
            if (!gameOverQuery.IsEmpty)
            {
                IsGameOver.Value = true;
                entityMgr.DestroyEntity(gameOverQuery);
            }
        }

        public void RequestRestart()
        {
            if (!HasSingleton<GridResetRequest>())
                entityMgr.CreateSingleton<GridResetRequest>();

            IsGameOver.Value = false;
            scoreController.ResetScore();
        }

        public void RequestStart()
        {
            if (!HasSingleton<GridStartRequest>())
                entityMgr.CreateSingleton<GridStartRequest>();
        }

        private bool HasSingleton<T>() where T : unmanaged, IComponentData
        {
            var query = entityMgr.CreateEntityQuery(typeof(T));
            return !query.IsEmpty;
        }

        public void Dispose()
        {
            disposables?.Dispose();
            CurrentPhase?.Dispose();
            IsGameOver?.Dispose();
        }
    }
}
