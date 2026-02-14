using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Interfaces;
using UnityEngine;
using VContainer;

namespace Match3.Save
{
    /// <summary>
    /// ISaveController implementation using Unity's PlayerPrefs.
    /// Simple key-value storage, suitable for small save data.
    /// For larger/complex saves, consider a file-based implementation.
    /// </summary>
    public class PlayerPrefsSaveController : ISaveController
    {
        [Inject] private readonly ISerializer serializer;

        public async UniTask<T> LoadAsync<T>(string key, T fallback = default, CancellationToken ct = default) where T : class
        {
            await UniTask.Yield();
            ct.ThrowIfCancellationRequested();

            if (!PlayerPrefs.HasKey(key))
                return fallback;

            try
            {
                var raw = PlayerPrefs.GetString(key);
                var data = serializer.Deserialize<T>(raw);
                return data ?? fallback;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerPrefsSaveController] Failed to load '{key}': {e.Message}");
                return fallback;
            }
        }

        public async UniTask<bool> SaveAsync<T>(string key, T data, CancellationToken ct = default) where T : class
        {
            await UniTask.Yield();
            ct.ThrowIfCancellationRequested();

            try
            {
                var raw = serializer.Serialize(data);
                PlayerPrefs.SetString(key, raw);
                PlayerPrefs.Save();
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerPrefsSaveController] Failed to save '{key}': {e.Message}");
                return false;
            }
        }

        public async UniTask<bool> DeleteAsync(string key, CancellationToken ct = default)
        {
            await UniTask.Yield();
            ct.ThrowIfCancellationRequested();

            if (!PlayerPrefs.HasKey(key))
                return false;
            
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            return true;
        }
    }
}
