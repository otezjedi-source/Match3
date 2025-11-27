using Cysharp.Threading.Tasks;
using MiniIT.CONTROLLERS;
using MiniIT.SAVE;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MiniIT.CORE
{
    public class BootLifetimeScope : LifetimeScope {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private AudioSource audioSource;

        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(audioSource.gameObject);
            base.Awake();
        }

        void Start()
        {
            var sceneLoader = Container.Resolve<SceneLoader>();
            sceneLoader.LoadStartSceneAsync().Forget();
        }
        
        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterInstance(gameConfig);

            builder.Register<SceneLoader>(Lifetime.Singleton);
            builder.Register<ScoreController>(Lifetime.Singleton);
            builder.Register<SoundController>(Lifetime.Singleton)
                .WithParameter(audioSource)
                .WithParameter(gameConfig);
            builder.Register<ISaveController, PlayerPrefsSaveController>(Lifetime.Singleton);
        }
    }
}
