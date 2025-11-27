using MiniIT.CONTROLLERS;
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
            
            builder.Register<GridController>(Lifetime.Scoped);
            builder.Register<MatchController>(Lifetime.Scoped);
            builder.Register<GameStateMachine>(Lifetime.Scoped);
            builder.Register<InputController>(Lifetime.Scoped);

            builder.RegisterComponent(gameUI).AsSelf();
            builder.RegisterComponentInHierarchy<MenuGame>();
            builder.RegisterComponentInHierarchy<MenuGameOver>();
            
            builder.RegisterEntryPoint<GameInitializer>();
        }
    }
}
