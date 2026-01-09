using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Match3.Utils
{
    public class AssetHandle<T> where T : Object
    {
        private AssetReference assetRef;
        private AsyncOperationHandle<T> handle;
        private CancellationTokenSource cts;

        public async UniTask<T> LoadAsync(AssetReference newRef, CancellationToken ct = default)
        {
            if (newRef == null)
                return null;

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
