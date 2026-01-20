using Cysharp.Threading.Tasks;
using Match3.Controllers;
using Match3.ECS.Components;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Match3.UI
{
    /// <summary>
    /// In-game HUD with score display and back button.
    /// </summary>
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

            // Auto-update score display
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
            soundController.Play(SoundType.BtnClick);
            sceneLoader.LoadStartSceneAsync().Forget();
        }
    }
}
