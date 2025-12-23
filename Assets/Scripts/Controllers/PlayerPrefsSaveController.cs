using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Match3.Save
{
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
