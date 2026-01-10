using System;
using Cysharp.Threading.Tasks;
using Match3.Save;
using UniRx;
using UnityEngine;
using VContainer;

namespace Match3.Controllers
{
    public class ScoreController : IDisposable
    {
        [Inject] private readonly ISaveController saveController;

        public readonly ReactiveProperty<int> Score = new(0);
        public readonly ReactiveProperty<int> HighScore = new(0);
        public readonly ReactiveProperty<bool> IsReady = new(false);

        private readonly CompositeDisposable disposables = new();
        private SaveData saveData;

        public bool IsNewHighScore { get; private set; } = false;

        public ScoreController()
        {
            Score
                .Where(score => score > HighScore.Value)
                .Subscribe(score => SetHighScore(score))
                .AddTo(disposables);
        }

        public async UniTask InitAsync()
        {
            saveData = await saveController.LoadAsync();
            HighScore.Value = saveData.HighScore;
            IsReady.Value = true;
        }

        public void AddScore(int points)
        {
            Score.Value += points;
        }

        public void ResetScore()
        {
            Score.Value = 0;
            IsNewHighScore = false;
        }

        private void SetHighScore(int newHighScore)
        {
            HighScore.Value = newHighScore;
            saveData.HighScore = newHighScore;
            IsNewHighScore = true;
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

        public void Dispose()
        {
            disposables?.Dispose();
            Score?.Dispose();
            HighScore?.Dispose();
            IsReady?.Dispose();
        }
    }
}
