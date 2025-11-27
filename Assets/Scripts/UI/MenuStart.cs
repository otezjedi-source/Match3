using Cysharp.Threading.Tasks;
using MiniIT.CONTROLLERS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace MiniIT.UI
{
    public class MenuStart : MonoBehaviour
    {
        [SerializeField] private TMP_Text highScore;
        [SerializeField] private Button btnStart;
        [SerializeField] private Button btnResetHighScore;
        [SerializeField] private Button btnQuit;

        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly SceneLoader sceneLoader;
        [Inject] private readonly SoundController soundController;

        private void Start()
        {
            btnStart.onClick.AddListener(OnBtnStartClicked);
            btnResetHighScore.onClick.AddListener(OnBtnResetHighScoreClick);
            btnQuit.onClick.AddListener(OnBtnQuitClick);

            scoreController.OnHighScoreChanged.AddListener(OnHighScoreChanged);
            OnHighScoreChanged(scoreController.HighScore);
        }

        void OnDestroy()
        {
            scoreController.OnHighScoreChanged.RemoveListener(OnHighScoreChanged);
        }

        private void OnHighScoreChanged(int newScore)
        {
            highScore.text = $"High Score: {newScore}";
        }

        private void OnBtnStartClicked()
        {
            soundController.PlayBtnClick();
            sceneLoader.LoadGameSceneAsync().Forget();
        }

        private void OnBtnResetHighScoreClick()
        {
            soundController.PlayBtnClick();
            scoreController.ResetHighScore();
        }

        private void OnBtnQuitClick()
        {
            soundController.PlayBtnClick();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
