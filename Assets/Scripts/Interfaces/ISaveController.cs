using Cysharp.Threading.Tasks;

namespace MiniIT.SAVE
{
    public interface ISaveController
    {
        UniTask<SaveData> LoadAsync();
        UniTask SaveAsync(SaveData data);
        UniTask DeleteAsync();
    }
}
