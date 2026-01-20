using Match3.Controllers;
using UniRx;
using UnityEngine;
using VContainer;

namespace Match3.UI
{
    /// <summary>
    /// Main game UI. Manages menu visibility based on game state.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private MenuGame menuGame;
        [SerializeField] private MenuGameOver menuGameOver;
        [SerializeField] private GameObject grid;

        [Inject] private readonly GameController gameController;

        private void Start()
        {
            ShowGameMenu();

            // Switch to game over when game ends
            gameController.IsGameOver
                .Where(isGameOver => isGameOver)
                .Subscribe(_ => ShowGameOverMenu())
                .AddTo(this);
        }

        public void ShowGameMenu()
        {
            grid.SetActive(true);
            menuGame.gameObject.SetActive(true);
            menuGameOver.gameObject.SetActive(false);
        }

        public void ShowGameOverMenu()
        {
            grid.SetActive(false);
            menuGame.gameObject.SetActive(false);
            menuGameOver.gameObject.SetActive(true);
        }
    }
}
