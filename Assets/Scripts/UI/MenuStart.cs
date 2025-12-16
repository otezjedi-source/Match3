using Cysharp.Threading.Tasks;
using MiniIT.CONTROLLERS;
using TMPro;
using UniRx;
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
            btnStart.OnClickAsObservable()
                .Subscribe(_ => OnBtnStartClicked())
                .AddTo(this);

            btnResetHighScore.OnClickAsObservable()
                .Subscribe(_ => OnBtnResetHighScoreClick())
                .AddTo(this);

            btnQuit.OnClickAsObservable()
                .Subscribe(_ => OnBtnQuitClick())
                .AddTo(this);

            scoreController.HighScore
                .Subscribe(score => OnHighScoreChanged(score))
                .AddTo(this);
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
