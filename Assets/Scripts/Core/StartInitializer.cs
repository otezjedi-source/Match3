using Cysharp.Threading.Tasks;
using Match3.Controllers;
using VContainer;
using VContainer.Unity;

namespace Match3.Core
{
    public class StartInitializer : IStartable
    {
        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly LoadingController loadingController;
        
        public void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            using (loadingController.BeginLoading())
            {
                await scoreController.InitAsync();
            }
        }
    }
}
