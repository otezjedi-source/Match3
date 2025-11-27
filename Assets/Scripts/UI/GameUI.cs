using MiniIT.CONTROLLERS;
using MiniIT.GAME;
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
            scoreController.OnScoreChanged.AddListener(menuGame.UpdateScore);
            stateMachine.OnGameOver.AddListener(ShowGameOverMenu);

            ShowGameMenu();
        }

        private void OnDestroy()
        {
            scoreController.OnScoreChanged.RemoveListener(menuGame.UpdateScore);
            stateMachine.OnGameOver.RemoveListener(ShowGameOverMenu);
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
