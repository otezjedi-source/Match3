using Cysharp.Threading.Tasks;
using Match3.Controllers;
using Match3.ECS.Components;
using TMPro;
using UniRx;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Match3.UI
{
    /// <summary>
    /// Start menu with play button and high score display.
    /// </summary>
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
            soundController.Play(SoundType.BtnClick);
            sceneLoader.LoadGameSceneAsync().Forget();
        }

        private void OnBtnResetHighScoreClick()
        {
            soundController.Play(SoundType.BtnClick);
            scoreController.ResetHighScore();
        }

        private void OnBtnQuitClick()
        {
            soundController.Play(SoundType.BtnClick);

            // Cleanup ECS world
            if (World.DefaultGameObjectInjectionWorld?.IsCreated == true)
                World.DefaultGameObjectInjectionWorld.Dispose();
                
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
