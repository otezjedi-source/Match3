using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Match3.Controllers
{
    public sealed class SceneLoader : IDisposable
    {
        const string START_SCENE_NAME = "StartScene";
        const string GAME_SCENE_NAME = "GameScene";

        [Inject] private readonly LifetimeScope parentScope;

        private CancellationTokenSource cts;
        private int loadVersion;
        private bool isLoading;

        public UniTask LoadStartSceneAsync() => LoadSceneAsync(START_SCENE_NAME);
        public UniTask LoadGameSceneAsync() => LoadSceneAsync(GAME_SCENE_NAME);

        private async UniTask LoadSceneAsync(string sceneName)
        {
            if (isLoading || SceneManager.GetActiveScene().name == sceneName)
                return;

            CancelLoad();

            cts = new();
            var newVersion = ++loadVersion;
            isLoading = true;

            try
            {
                var op = SceneManager.LoadSceneAsync(sceneName);
                if (op == null)
                    throw new InvalidOperationException($"Failed to start loading scene: {sceneName}");

                using (LifetimeScope.EnqueueParent(parentScope))
                {
                    await op.ToUniTask(cancellationToken: cts.Token);
                }

                if (newVersion != loadVersion)
                    return;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[SceneLoader] Scene load cancelled: {sceneName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneLoader] Error loading scene {sceneName}\n{ex}");
            }
            finally
            {
                if (newVersion == loadVersion)
                    isLoading = false;
            }
        }
        
        private void CancelLoad()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        public void Dispose()
        {
            CancelLoad();
        }
    }
}
