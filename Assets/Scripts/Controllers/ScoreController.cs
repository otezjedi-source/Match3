using System;
using Cysharp.Threading.Tasks;
using MiniIT.SAVE;
using UnityEngine;
using UnityEngine.Events;
using VContainer;

namespace MiniIT.CONTROLLERS
{
    public class ScoreController
    {
        [Inject] private readonly ISaveController saveController;

        private SaveData saveData;

        private int score;
        public int Score
        {
            get => score;
            private set
            {
                score = value;
                OnScoreChanged?.Invoke(score);

                if (score > highScore)
                {
                    SetHighScore(score);
                    IsNewHighScore = true;
                }
            }
        }

        private int highScore;
        public int HighScore
        {
            get => highScore;
            private set
            {
                highScore = value;
                OnHighScoreChanged?.Invoke(highScore);
            }
        }

        public bool IsNewHighScore { get; private set; } = false;

        public UnityEvent<int> OnScoreChanged = null;
        public UnityEvent<int> OnHighScoreChanged = null;

        private ScoreController()
        {
            OnScoreChanged = new UnityEvent<int>();
            OnHighScoreChanged = new UnityEvent<int>();
        }

        public async UniTask InitAsync()
        {
            saveData = await saveController.LoadAsync();
            HighScore = saveData.HighScore;
        }

        public void AddScore(int points)
        {
            Score += points;
        }

        public void ResetScore()
        {
            Score = 0;
            IsNewHighScore = false;
        }

        private void SetHighScore(int newHighScore)
        {
            HighScore = newHighScore;
            saveData.HighScore = newHighScore;
            SaveAsync().Forget();
        }

        public void ResetHighScore()
        {
            SetHighScore(0);
        }

        private async UniTaskVoid SaveAsync()
        {
            try
            {
                await saveController.SaveAsync(saveData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save: {ex.Message}");
            }
        }
    }
}
