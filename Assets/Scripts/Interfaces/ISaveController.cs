using Cysharp.Threading.Tasks;

namespace Match3.Save
{
    /// <summary>
    /// Interface for save/load operations.
    /// Allows swapping implementations (PlayerPrefs, file, cloud, etc.)
    /// </summary>
    public interface ISaveController
    {
        UniTask<SaveData> LoadAsync();
        UniTask SaveAsync(SaveData data);
        UniTask DeleteAsync();
    }
}
