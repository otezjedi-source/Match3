using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Match3.Utils
{
    /// <summary>
    /// Wrapper for Addressables async loading with automatic cleanup.
    /// Handles cancellation, caching, and proper release of loaded assets.
    /// </summary>
    /// <typeparam name="T">Asset type to load (Sprite, SkeletonDataAsset, etc.)</typeparam>
    public class AssetHandle<T> where T : Object
    {
        private AssetReference assetRef;
        private AsyncOperationHandle<T> handle;
        private CancellationTokenSource cts;

        /// <summary>
        /// Load an asset asynchronously. If the same asset is already loaded, returns cached result.
        /// </summary>
        /// <param name="newRef">Addressable reference to load</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Loaded asset or null if cancelled/failed</returns>
        public async UniTask<T> LoadAsync(AssetReference newRef, CancellationToken ct = default)
        {
            if (newRef == null)
                return null;

            // Return cached result if same asset is already loaded
            if (assetRef != null && newRef.AssetGUID == assetRef.AssetGUID && handle.IsValid() && handle.IsDone)
                return handle.Result;

            Cancel();

            assetRef = newRef;
            cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            handle = Addressables.LoadAssetAsync<T>(newRef);

            try
            {
                await handle.ToUniTask(cancellationToken: cts.Token);
                return handle.Result;
            }
            catch
            {
                Cancel();
                return null;
            }
        }

        /// <summary>
        /// Release the loaded asset and cleanup resources.
        /// </summary>
        public void Release()
        {
            Cancel();
        }

        private void Cancel()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;

            if (handle.IsValid())
                Addressables.Release(handle);

            handle = default;
            assetRef = null;
        }
    }
}
