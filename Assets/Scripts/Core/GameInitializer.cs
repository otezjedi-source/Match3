using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Controllers;
using Match3.Data;
using Match3.ECS.Components;
using Match3.ECS.Systems;
using Match3.Factories;
using Unity.Entities;
using VContainer;
using VContainer.Unity;

namespace Match3.Core
{
    /// <summary>
    /// Entry point for the game scene. Initializes ECS systems and controllers.
    /// Implements VContainer interfaces for automatic lifecycle management.
    /// </summary>
    public class GameInitializer : IStartable, ITickable, IDisposable
    {
        [Inject] private readonly InputController inputController;
        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly SoundController soundController;
        [Inject] private readonly GameController gameController;
        [Inject] private readonly LoadingController loadingController;
        [Inject] private readonly TileTypeRegistry tileTypeRegistry;
        [Inject] private readonly DataCache dataCache;
        [Inject] private readonly TileFactory tileFactory;

        private World world;
        private EntityManager entityManager;
        private CancellationTokenSource cts;

        /// <summary>
        /// Called by VContainer after all dependencies are injected.
        /// </summary>
        public void Start()
        {
            cts = new();
            InitAsync(cts.Token).Forget();
        }
        
        private async UniTaskVoid InitAsync(CancellationToken ct)
        {
            using (loadingController.BeginLoading())
            {
                world = World.DefaultGameObjectInjectionWorld;
                entityManager = world.EntityManager;

                // Create singleton entity with references to managed objects
                // This allows ECS systems to access controllers and factories
                CreateManagedRefs();
                EnableSystems(true);

                // Initialize controllers
                inputController.Init();
                gameController.Init();
                tileFactory.Init();

                // Request initial grid generation
                gameController.RequestStart();

                // Wait until grid is ready (GridStartRequest consumed)
                var query = entityManager.CreateEntityQuery(typeof(GridStartRequest));
                try
                {
                    await UniTask.WaitUntil(() => query.IsEmpty, cancellationToken: ct);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    query.Dispose();
                }

                try
                {
                    await tileFactory.WaitForLoading(ct);
                }
                catch (OperationCanceledException) { }
            }
        }

        /// <summary>
        /// Called by VContainer every frame.
        /// </summary>
        public void Tick()
        {
            inputController.Update();
        }

        public void Dispose()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            
            EnableSystems(false);
        }

        /// <summary>
        /// Create or update the ManagedReferences singleton that ECS systems use
        /// to access managed objects (controllers, factories).
        /// </summary>
        private void CreateManagedRefs()
        {
            var query = entityManager.CreateEntityQuery(typeof(ManagedReferences));
            if (!query.IsEmpty)
            {
                var entity = query.GetSingletonEntity();
                var refs = entityManager.GetComponentObject<ManagedReferences>(entity);
                refs.scoreController = scoreController;
                refs.soundController = soundController;
                refs.tileTypeRegistry = tileTypeRegistry;
                refs.dataCache = dataCache;
                refs.tileFactory = tileFactory;
                query.Dispose();
                return;
            }

            // Create new singleton
            var newEntity = entityManager.CreateEntity();
            entityManager.AddComponentObject(newEntity, new ManagedReferences
            {
                scoreController = scoreController,
                soundController = soundController,
                tileTypeRegistry = tileTypeRegistry,
                dataCache = dataCache,
                tileFactory = tileFactory,
            });
            query.Dispose();
        }

        /// <summary>
        /// Enable/disable game system groups. Systems are disabled by default
        /// and only enabled when the game scene is active.
        /// </summary>
        private void EnableSystems(bool enabled)
        {
            if (world?.IsCreated != true)
                return;

            var initGroup = world.GetExistingSystemManaged<GameInitSystemGroup>();
            if (initGroup != null)
                initGroup.Enabled = enabled;

            var gameGroup = world.GetExistingSystemManaged<GameSystemGroup>();
            if (gameGroup != null)
                gameGroup.Enabled = enabled;

            var syncGroup = world.GetExistingSystemManaged<GameSyncSystemGroup>();
            if (syncGroup != null)
                syncGroup.Enabled = enabled;
        }
    }
}
