using Match3.Controllers;
using Match3.Factories;
using Match3.Game;
using Match3.UI;
using Unity.Entities;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Match3.Core
{
    public class GameLifetimeScope : LifetimeScope
     {
        [SerializeField] private Transform gridParent;
        [SerializeField] private Tile tilePrefab;
        [SerializeField] private GameUI gameUI;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(World.DefaultGameObjectInjectionWorld.EntityManager);
            
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
