using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Match3.Controllers
{
    public class SceneLoader
    {
        const string START_SCENE_NAME = "StartScene";
        const string GAME_SCENE_NAME = "GameScene";

        [Inject] private readonly LifetimeScope parentScope;

        public async UniTask LoadStartSceneAsync() => await LoadSceneAsync(START_SCENE_NAME);
        public async UniTask LoadGameSceneAsync() => await LoadSceneAsync(GAME_SCENE_NAME);

        private async UniTask LoadSceneAsync(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"Failed to load scene: {sceneName}");
                return;
            }

            using (LifetimeScope.EnqueueParent(parentScope))
            {
                await operation.ToUniTask();
            }
        }
    }
}
