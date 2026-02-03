using Cysharp.Threading.Tasks;
using Match3.Data;
using UnityEngine;

namespace Match3.Save
{
    /// <summary>
    /// ISaveController implementation using Unity's PlayerPrefs.
    /// Simple key-value storage, suitable for small save data.
    /// For larger/complex saves, consider a file-based implementation.
    /// </summary>
    public class PlayerPrefsSaveController : ISaveController
    {
        private const string SAVE_KEY = "SaveData";

        public async UniTask<SaveData> LoadAsync()
        {
            await UniTask.Yield();

            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                var json = PlayerPrefs.GetString(SAVE_KEY);
                return SaveData.FromJson(json);
            }

            return new SaveData();
        }

        public async UniTask SaveAsync(SaveData data)
        {
            await UniTask.Yield();

            var json = data.ToJson();
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public async UniTask DeleteAsync()
        {
            await UniTask.Yield();

            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                PlayerPrefs.DeleteKey(SAVE_KEY);
                PlayerPrefs.Save();
            }
        }
    }
}
