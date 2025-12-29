using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Match3.Utils
{
    public class AssetHandle<T>
    {
        private AssetReference assetRef;
        private AsyncOperationHandle<T> handle;

        public async UniTask<T> LoadAsync(AssetReference newRef)
        {
            if (newRef.AssetGUID == assetRef?.AssetGUID && handle.IsValid() && handle.IsDone)
                return handle.Result;

            Release();

            assetRef = newRef;
            handle = Addressables.LoadAssetAsync<T>(newRef);
            await handle;

            return handle.Result;
        }

        public void Release()
        {
            if (handle.IsValid())
                Addressables.Release(handle);
            handle = default;
        }
    }
}
