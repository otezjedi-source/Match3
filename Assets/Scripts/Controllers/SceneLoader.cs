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
        [Inject] private readonly LoadingController loadingController;

        private CancellationTokenSource cts;
        private readonly SemaphoreSlim semaphore = new(1, 1);

        public UniTask LoadStartSceneAsync() => LoadSceneAsync(START_SCENE_NAME);
        public UniTask LoadGameSceneAsync() => LoadSceneAsync(GAME_SCENE_NAME);

        private async UniTask LoadSceneAsync(string sceneName)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
                return;

            await semaphore.WaitAsync();

            using (loadingController.BeginLoading())
            {
                CancellationTokenSource opCts = null;

                try
                {
                    CancelLoad();
                    opCts = cts = new();

                    var op = SceneManager.LoadSceneAsync(sceneName);
                    if (op == null)
                        throw new InvalidOperationException($"[SceneLoader] Failed to start loading scene: {sceneName}");

                    using (LifetimeScope.EnqueueParent(parentScope))
                    {
                        await op.ToUniTask(cancellationToken: opCts.Token);
                    }
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
                    semaphore.Release();
                }
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
            semaphore.Dispose();
        }
    }
}
