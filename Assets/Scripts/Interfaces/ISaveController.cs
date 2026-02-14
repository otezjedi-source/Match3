using System.Threading;
using Cysharp.Threading.Tasks;

namespace Match3.Save
{
    /// <summary>
    /// Interface for save/load operations.
    /// Allows swapping implementations (PlayerPrefs, file, cloud, etc.)
    /// </summary>
    public interface ISaveController
    {
        UniTask<T> LoadAsync<T>(string key, T fallback = default, CancellationToken ct = default) where T : class;
        UniTask<bool> SaveAsync<T>(string key, T data, CancellationToken ct = default) where T : class;
        UniTask<bool> DeleteAsync(string key, CancellationToken ct = default);
    }
}
