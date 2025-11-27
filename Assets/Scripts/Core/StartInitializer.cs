using Cysharp.Threading.Tasks;
using MiniIT.CONTROLLERS;
using VContainer;
using VContainer.Unity;

namespace MiniIT.CORE
{
    public class StartInitializer : IStartable
    {
        [Inject] private readonly ScoreController scoreController;
        
        public void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            await scoreController.InitAsync();
        }
    }
}
