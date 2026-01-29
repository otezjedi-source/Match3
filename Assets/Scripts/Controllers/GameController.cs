using System;
using System.Collections.Generic;
using Match3.ECS.Components;
using UniRx;
using Unity.Entities;
using VContainer;

namespace Match3.Controllers
{
    /// <summary>
    /// Bridge between ECS game state and the rest of the application.
    /// Polls ECS singletons and exposes them as reactive properties for UI binding.
    /// </summary>
    public class GameController : IDisposable
    {
        [Inject] private readonly EntityManager entityManager;
        [Inject] private readonly ScoreController scoreController;

        public readonly ReactiveProperty<GamePhase> CurrentPhase = new(GamePhase.Idle);
        public readonly ReactiveProperty<bool> IsGameOver = new(false);

        private readonly Dictionary<Type, EntityQuery> queryCache = new();
        private readonly CompositeDisposable disposables = new();

        private bool isDisposed;

        private static bool WorldExists => World.DefaultGameObjectInjectionWorld?.IsCreated == true;

        public void Init()
        {
            if (isDisposed)
                throw new ObjectDisposedException("[GameController] Trying to init disposed");

            ClearQueries();
            CacheQuery<GameState>();
            CacheQuery<GameOverEvent>();
            CacheQuery<GridStartRequest>();
            CacheQuery<GridResetRequest>();

            // Poll ECS state every frame and sync to reactive properties
            Observable.EveryUpdate()
                .Where(_ => !isDisposed)
                .Subscribe(_ => UpdateGameState())
                .AddTo(disposables);
        }

        private void UpdateGameState()
        {
            if (isDisposed || !WorldExists)
                return;

            // Sync game phase from ECS singleton
            var gameStateQuery = GetQuery<GameState>();
            if (!gameStateQuery.IsEmpty)
            {
                gameStateQuery.CompleteDependency();
                CurrentPhase.Value = gameStateQuery.GetSingleton<GameState>().phase;
            }

            // Check for game over event (one-shot entity)
            var gameOverQuery = GetQuery<GameOverEvent>();
            if (!gameOverQuery.IsEmpty)
            {
                gameOverQuery.CompleteDependency();
                IsGameOver.Value = true;
                entityManager.DestroyEntity(gameOverQuery);
            }
        }

        /// <summary>
        /// Request initial grid generation. Used on first load.
        /// </summary>
        public void RequestStart()
        {
            if (isDisposed || !WorldExists)
                return;

            var gridStartQuery = GetQuery<GridStartRequest>();
            if (gridStartQuery.IsEmpty)
                entityManager.CreateSingleton<GridStartRequest>();
        }

        /// <summary>
        /// Request grid reset. Creates a singleton entity that GridResetSystem will process.
        /// </summary>
        public void RequestRestart()
        {
            if (isDisposed || !WorldExists)
                return;

            var gridResetQuery = GetQuery<GridResetRequest>();
            if (gridResetQuery.IsEmpty)
                entityManager.CreateSingleton<GridResetRequest>();

            IsGameOver.Value = false;
            scoreController.ResetScore();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            // Order matters: stop polling first, then cleanup
            disposables?.Dispose();
            CurrentPhase?.Dispose();
            IsGameOver?.Dispose();
            ClearQueries();
            
            if (WorldExists)
            {
                World.DefaultGameObjectInjectionWorld.Dispose();
                DefaultWorldInitialization.Initialize("Default world");
            }
        }

        private void CacheQuery<T>() where T : unmanaged, IComponentData
        {
            var type = typeof(T);
            if (!queryCache.ContainsKey(type))
                queryCache[type] = entityManager.CreateEntityQuery(typeof(T));
        }

        private EntityQuery GetQuery<T>() where T : unmanaged, IComponentData
        {
            if (!queryCache.TryGetValue(typeof(T), out var query))
                throw new InvalidOperationException($"[GameController] Query for {typeof(T)} not cached. Call CacheQuery first.");
            return query;
        }

        private void ClearQueries()
        {
            if (!WorldExists)
            {
                queryCache.Clear();
                return;
            }
                
            foreach (var query in queryCache.Values)
            {
                if (!query.Equals(default))
                    query.Dispose();
            }
            queryCache.Clear();
        }
    }
}
