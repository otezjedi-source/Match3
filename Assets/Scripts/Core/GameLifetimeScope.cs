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
    /// <summary>
    /// DI container for the game scene. Child of BootLifetimeScope.
    /// Registers game-specific services with Scoped lifetime (destroyed on scene unload).
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
     {
        [SerializeField] private Transform gridParent;
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private GameUI gameUI;

        protected override void Configure(IContainerBuilder builder)
        {
            // ECS World must exist before we can use EntityManager
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                throw new InvalidOperationException("[GameLifetimeScope] ECS World not initialized");

            builder.RegisterInstance(world.EntityManager);
            
            // Tile factory with scene-specific dependencies
            builder.Register<TileFactory>(Lifetime.Scoped)
                .WithParameter(tilePrefab)
                .WithParameter(gridParent);

            // Game-scene controllers (Scoped = destroyed when scene unloads)
            builder.Register<InputController>(Lifetime.Scoped);
            builder.Register<GameController>(Lifetime.Scoped);
            builder.Register<TileTypeRegistry>(Lifetime.Scoped);

            // UI components
            builder.RegisterComponent(gameUI).AsSelf();
            builder.RegisterComponentInHierarchy<MenuGame>();
            builder.RegisterComponentInHierarchy<MenuGameOver>();
            
            // Entry point - IStartable.Start() and ITickable.Tick() called by VContainer
            builder.RegisterEntryPoint<GameInitializer>();
        }
    }
}
