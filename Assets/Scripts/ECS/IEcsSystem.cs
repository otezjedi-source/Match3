using Cysharp.Threading.Tasks;

namespace MiniIT.ECS
{
    public interface IEcsSystem
    {
        void Initialize();
        void Execute();
        void Cleanup();
    }

    public interface IEcsAsyncSystem
    {
        void Initialize();
        UniTask ExecuteAsync();
        void Cleanup();
    }
}
