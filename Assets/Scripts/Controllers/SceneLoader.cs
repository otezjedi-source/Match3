using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Match3.Controllers
{
    /// <summary>
    /// Handles async scene loading with VContainer parent scope propagation.
    /// Ensures child LifetimeScopes inherit from the boot scope.
    /// </summary>
    public sealed class SceneLoader : IDisposable
    {
        const string START_SCENE_NAME = "StartScene";
        const string GAME_SCENE_NAME = "GameScene";

        [Inject] private readonly LifetimeScope parentScope;
        [Inject] private readonly LoadingController loadingController;

        private readonly SemaphoreSlim semaphore = new(1, 1); // Prevent concurrent loads
        private bool isDisposed;

        public UniTask LoadStartSceneAsync() => LoadSceneAsync(START_SCENE_NAME);
        public UniTask LoadGameSceneAsync() => LoadSceneAsync(GAME_SCENE_NAME);

        private async UniTask LoadSceneAsync(string sceneName)
        {
            if (isDisposed)
                return;

            // Skip if already on target scene
            if (SceneManager.GetActiveScene().name == sceneName)
                return;

            await semaphore.WaitAsync();

            try
            {
                using (loadingController.BeginLoading())
                {
                    var op = SceneManager.LoadSceneAsync(sceneName);
                    if (op == null)
                        throw new InvalidOperationException($"[SceneLoader] Failed to start loading scene: {sceneName}");

                    // EnqueueParent ensures new scene's LifetimeScope inherits from boot scope
                    using (LifetimeScope.EnqueueParent(parentScope))
                        await op.ToUniTask();
                }
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
        
        public void Dispose()
        {
            isDisposed = true;
            semaphore.Dispose();
        }
    }
}
