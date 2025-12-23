using MiniIT.CONTROLLERS;
using MiniIT.ECS;
using MiniIT.ECS.Systems;
using MiniIT.FACTORIES;
using MiniIT.GAME;
using MiniIT.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MiniIT.CORE
{
    public class GameLifetimeScope : LifetimeScope {
        [SerializeField] private Transform gridParent;
        [SerializeField] private Cell cellPrefab;
        [SerializeField] private Tile tilePrefab;
        [SerializeField] private GameUI gameUI;

        protected override void Configure(IContainerBuilder builder) {
            builder.Register<CellFactory>(Lifetime.Scoped)
                .WithParameter(cellPrefab)
                .WithParameter(gridParent);

            builder.Register<TileFactory>(Lifetime.Scoped)
                .WithParameter(tilePrefab)
                .WithParameter(gridParent);

            builder.Register<EcsWorld>(Lifetime.Scoped);
            builder.Register<GridInitializationSystem>(Lifetime.Scoped);
            builder.Register<SwapSystem>(Lifetime.Scoped);
            builder.Register<MatchDetectionSystem>(Lifetime.Scoped);
            builder.Register<DestroySystem>(Lifetime.Scoped);
            builder.Register<FallSystem>(Lifetime.Scoped);
            builder.Register<FillSystem>(Lifetime.Scoped);
            builder.Register<InputSystem>(Lifetime.Scoped);

            builder.Register<GameStateMachine>(Lifetime.Scoped);

            builder.RegisterComponent(gameUI).AsSelf();
            builder.RegisterComponentInHierarchy<MenuGame>();
            builder.RegisterComponentInHierarchy<MenuGameOver>();

            builder.RegisterEntryPoint<GameInitializer>();
        }
    }
}
