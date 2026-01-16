using Cysharp.Threading.Tasks;
using Match3.Controllers;
using Match3.Save;
using Match3.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Match3.Core
{
    /// <summary>
    /// Root DI container. Lives for entire application lifetime.
    /// Registers global services: scene loader, audio, save system, etc.
    /// </summary>
    public class BootLifetimeScope : LifetimeScope {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private LoadingScreen loadingScreen;

        protected override void Awake()
        {
            // Persist across scene loads
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(audioSource.gameObject);
            DontDestroyOnLoad(loadingScreen.gameObject);
            base.Awake();
        }

        void Start()
        {
            // Kick off the game by loading the start scene
            var sceneLoader = Container.Resolve<SceneLoader>();
            sceneLoader.LoadStartSceneAsync().Forget();
        }
        
        protected override void Configure(IContainerBuilder builder)
        {
            // Config as singleton instance (ScriptableObject)
            builder.RegisterInstance(gameConfig);

            // Core services - Singleton lifetime (one instance for entire app)
            builder.Register<SceneLoader>(Lifetime.Singleton);
            builder.Register<LoadingController>(Lifetime.Singleton);
            builder.Register<ScoreController>(Lifetime.Singleton);
            builder.Register<SoundController>(Lifetime.Singleton)
                .WithParameter(audioSource)
                .WithParameter(gameConfig);

            // Save system - interface binding allows swapping implementations
            builder.Register<ISaveController, PlayerPrefsSaveController>(Lifetime.Singleton);

            // UI component (already in scene, just register for injection)
            builder.RegisterComponent(loadingScreen);
        }
    }
}
