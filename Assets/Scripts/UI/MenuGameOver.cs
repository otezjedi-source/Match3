using Cysharp.Threading.Tasks;
using MiniIT.CONTROLLERS;
using TMPro;
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
        [Inject] private readonly GridController gridController;
        [Inject] private readonly SoundController soundController;
        [Inject] private readonly GameUI gameUI;

        public void Start()
        {
            btnBack.onClick.AddListener(OnBtnBackClick);
            btnRestart.onClick.AddListener(OnBtnRestartClick);
        }

        private void OnEnable()
        {
            score.text = $"Final Score: {scoreController.Score}";
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
            gridController.ResetTiles();
            gameUI.ShowGameMenu();
        }
    }
}
