using Cysharp.Threading.Tasks;
using MiniIT.CONTROLLERS;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace MiniIT.UI
{
    public class MenuGame : MonoBehaviour
    {
        [SerializeField] private TMP_Text score;
        [SerializeField] private Button btnBack;

        [Inject] private readonly SceneLoader sceneLoader;
        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly SoundController soundController;

        public void Start()
        {
            btnBack.OnClickAsObservable()
                .Subscribe(_ => OnBtnBackClick())
                .AddTo(this);

            scoreController.Score
                .Subscribe(score => UpdateScore(score))
                .AddTo(this);
        }

        private void UpdateScore(int newScore)
        {
            score.text = $"Score: {newScore}";
        }

        private void OnBtnBackClick()
        {
            soundController.PlayBtnClick();
            sceneLoader.LoadStartSceneAsync().Forget();
        }
    }
}
