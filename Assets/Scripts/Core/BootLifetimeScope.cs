using Cysharp.Threading.Tasks;
using Match3.Controllers;
using Match3.Save;
using Match3.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Match3.Core
{
    public class BootLifetimeScope : LifetimeScope {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private LoadingScreen loadingScreen;

        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(audioSource.gameObject);
            DontDestroyOnLoad(loadingScreen.gameObject);
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
            builder.Register<LoadingController>(Lifetime.Singleton);
            builder.Register<ScoreController>(Lifetime.Singleton);
            builder.Register<SoundController>(Lifetime.Singleton)
                .WithParameter(audioSource)
                .WithParameter(gameConfig);
            builder.Register<ISaveController, PlayerPrefsSaveController>(Lifetime.Singleton);

            builder.RegisterComponent(loadingScreen);
        }
    }
}
