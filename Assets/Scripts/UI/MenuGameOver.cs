using Cysharp.Threading.Tasks;
using Match3.Controllers;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Match3.UI
{
    /// <summary>
    /// Game over screen with final score and restart option.
    /// </summary>
    public class MenuGameOver : MonoBehaviour
    {
        [SerializeField] private TMP_Text score;
        [SerializeField] private GameObject newHighScore;
        [SerializeField] private Button btnBack;
        [SerializeField] private Button btnRestart;

        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly SceneLoader sceneLoader;
        [Inject] private readonly SoundController soundController;
        [Inject] private readonly GameController gameController;
        [Inject] private readonly GameUI gameUI;

        public void Start()
        {
            btnBack.OnClickAsObservable()
                .Subscribe(_ => OnBtnBackClick())
                .AddTo(this);

            btnRestart.OnClickAsObservable()
                .Subscribe(_ => OnBtnRestartClick())
                .AddTo(this);
        }

        private void OnEnable()
        {
            score.text = $"Final Score: {scoreController.Score.Value}";
            newHighScore.SetActive(scoreController.IsNewHighScore);
        }

        private void OnBtnBackClick()
        {
            soundController.PlayBtnClick();
            sceneLoader.LoadStartSceneAsync().Forget();
        }

        private void OnBtnRestartClick()
        {
            soundController.PlayBtnClick();
            gameController.RequestRestart();
            gameUI.ShowGameMenu();
        }
    }
}
