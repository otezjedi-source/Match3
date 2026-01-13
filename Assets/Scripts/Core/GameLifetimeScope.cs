using System;
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
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private GameUI gameUI;

        protected override void Configure(IContainerBuilder builder)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                throw new InvalidOperationException("[GameLifetimeScope] ECS World not initialized");
            
            builder.RegisterInstance(world.EntityManager);
            
            builder.Register<TileFactory>(Lifetime.Scoped)
                .WithParameter(tilePrefab)
                .WithParameter(gridParent);

            builder.Register<InputController>(Lifetime.Scoped);
            builder.Register<GameController>(Lifetime.Scoped);
            builder.Register<TileTypeRegistry>(Lifetime.Scoped);

            builder.RegisterComponent(gameUI).AsSelf();
            builder.RegisterComponentInHierarchy<MenuGame>();
            builder.RegisterComponentInHierarchy<MenuGameOver>();
            
            builder.RegisterEntryPoint<GameInitializer>();
        }
    }
}
