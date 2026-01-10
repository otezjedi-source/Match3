using System;
using System.Collections.Generic;
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

        public readonly ReactiveProperty<GamePhase> CurrentPhase = new(GamePhase.Idle);
        public readonly ReactiveProperty<bool> IsGameOver = new(false);

        private readonly Dictionary<Type, EntityQuery> queryCache = new();
        private readonly CompositeDisposable disposables = new();

        public void Init()
        {
            CacheQuery<GameState>();
            CacheQuery<GameOverEvent>();
            CacheQuery<GridStartRequest>();
            CacheQuery<GridResetRequest>();

            Observable.EveryUpdate()
                .Subscribe(_ => UpdateGameState())
                .AddTo(disposables);
        }

        private void UpdateGameState()
        {
            var query = GetQuery<GameState>();
            if (!query.IsEmpty)
                CurrentPhase.Value = query.GetSingleton<GameState>().Phase;

            query = GetQuery<GameOverEvent>();
            if (!query.IsEmpty)
            {
                IsGameOver.Value = true;
                entityMgr.DestroyEntity(query);
            }
        }

        public void RequestRestart()
        {
            if (GetQuery<GridResetRequest>().IsEmpty)
                entityMgr.CreateSingleton<GridResetRequest>();

            IsGameOver.Value = false;
            scoreController.ResetScore();
        }

        public void RequestStart()
        {
            if (GetQuery<GridStartRequest>().IsEmpty)
                entityMgr.CreateSingleton<GridStartRequest>();
        }

        public void Dispose()
        {
            ClearQueries();

            CurrentPhase?.Dispose();
            IsGameOver?.Dispose();
            disposables?.Dispose();
        }

        private void CacheQuery<T>() where T : unmanaged, IComponentData
        {
            var type = typeof(T);
            if (!queryCache.ContainsKey(type))
                queryCache[type] = entityMgr.CreateEntityQuery(typeof(T));
        }

        private EntityQuery GetQuery<T>() where T : unmanaged, IComponentData
        {
            queryCache.TryGetValue(typeof(T), out var query);
            return query;
        }

        private void ClearQueries()
        {
            foreach (var query in queryCache.Values)
            {
                if (!query.Equals(default))
                    query.Dispose();
            }
            queryCache.Clear();
        }
    }
}
