using Cysharp.Threading.Tasks;

namespace Match3.Save
{
    public interface ISaveController
    {
        UniTask<SaveData> LoadAsync();
        UniTask SaveAsync(SaveData data);
        UniTask DeleteAsync();
    }
}
