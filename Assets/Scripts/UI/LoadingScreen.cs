using Match3.Controllers;
using UniRx;
using UnityEngine;
using VContainer;

namespace Match3.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        [Inject] private readonly LoadingController loadingController;

        private void Start()
        {
            loadingController.IsLoading
                .Subscribe(SetVisible)
                .AddTo(this);
        }

        private void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
    }
}
