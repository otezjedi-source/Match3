using MiniIT.CONTROLLERS;
using MiniIT.GAME;
using UniRx;
using UnityEngine;
using VContainer;

namespace MiniIT.UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private MenuGame menuGame;
        [SerializeField] private MenuGameOver menuGameOver;
        [SerializeField] private GameObject grid;

        [Inject] private readonly ScoreController scoreController;
        [Inject] private readonly GameStateMachine stateMachine;

        private void Start()
        {
            stateMachine.OnGameOver
                .Subscribe(_ => ShowGameOverMenu())
                .AddTo(this);

            ShowGameMenu();
        }

        public void ShowGameMenu()
        {
            grid.SetActive(true);
            menuGame.gameObject.SetActive(true);
            menuGameOver.gameObject.SetActive(false);

            scoreController.ResetScore();
        }

        public void ShowGameOverMenu()
        {
            grid.SetActive(false);
            menuGame.gameObject.SetActive(false);
            menuGameOver.gameObject.SetActive(true);
        }
    }
}
