using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Controllers;
using Match3.ECS.Components;
using Match3.ECS.Systems;
using Match3.Factories;
using Unity.Entities;
using VContainer;
using VContainer.Unity;

namespace Match3.Core
{
    public class GameInitializer : IStartable, ITickable, IDisposable
    {
        [Inject] private readonly InputController inputController;
        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly SoundController soundController;
        [Inject] private readonly GameController gameController;
        [Inject] private readonly LoadingController loadingController;
        [Inject] private readonly TileTypeRegistry tileTypeRegistry;
        [Inject] private readonly TileFactory tileFactory;

        private World world;
        private EntityManager entityMgr;
        private CancellationTokenSource cts;

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
                entityMgr = world.EntityManager;

                CreateManagedRefs();
                EnableSystems(true);

                inputController.Init();
                gameController.Init();

                gameController.RequestStart();

                var query = entityMgr.CreateEntityQuery(typeof(GridStartRequest));
                try
                {
                    await UniTask.WaitUntil(() => query.IsEmpty, cancellationToken: ct);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    query.Dispose();
                }
            }
        }

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

        private void CreateManagedRefs()
        {
            var query = entityMgr.CreateEntityQuery(typeof(ManagedReferences));
            if (!query.IsEmpty)
            {
                var entity = query.GetSingletonEntity();
                var refs = entityMgr.GetComponentObject<ManagedReferences>(entity);
                refs.ScoreController = scoreController;
                refs.SoundController = soundController;
                refs.TileTypeRegistry = tileTypeRegistry;
                refs.TileFactory = tileFactory;
                query.Dispose();
                return;
            }

            var newEntity = entityMgr.CreateEntity();
            entityMgr.AddComponentObject(newEntity, new ManagedReferences
            {
                ScoreController = scoreController,
                SoundController = soundController,
                TileTypeRegistry = tileTypeRegistry,
                TileFactory = tileFactory,
            });
            query.Dispose();
        }

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
        }
    }
}
