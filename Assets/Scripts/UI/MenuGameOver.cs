using Cysharp.Threading.Tasks;
using MiniIT.CONTROLLERS;
using MiniIT.ECS.Systems;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace MiniIT.UI
{
    public class MenuGameOver : MonoBehaviour
    {
        [SerializeField] private TMP_Text score;
        [SerializeField] private GameObject newHighScore;
        [SerializeField] private Button btnBack;
        [SerializeField] private Button btnRestart;

        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly SceneLoader sceneLoader;
        [Inject] private readonly GridInitializationSystem gridSystem;
        [Inject] private readonly MatchDetectionSystem matchSystem;
        [Inject] private readonly SoundController soundController;
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
            gridSystem.ResetTiles();
            matchSystem.InvalidateHasPossibleMoves();
            scoreController.ResetScore();
            gameUI.ShowGameMenu();
        }
    }
}
