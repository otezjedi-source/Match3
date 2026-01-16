using Cysharp.Threading.Tasks;
using Match3.Controllers;
using VContainer;
using VContainer.Unity;

namespace Match3.Core
{
    /// <summary>
    /// Entry point for the start/menu scene.
    /// Loads saved data before showing the menu.
    /// </summary>
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
                // Load high score from save file
                await scoreController.InitAsync();
            }
        }
    }
}
