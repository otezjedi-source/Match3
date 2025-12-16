using System;
using Cysharp.Threading.Tasks;
using MiniIT.SAVE;
using UniRx;
using UnityEngine;
using VContainer;

namespace MiniIT.CONTROLLERS
{
    public class ScoreController : IDisposable
    {
        [Inject] private readonly ISaveController saveController;

        public readonly ReactiveProperty<int> Score = null;
        public readonly ReactiveProperty<int> HighScore = null;

        private readonly CompositeDisposable disposables = null;
        private SaveData saveData;

        public bool IsNewHighScore { get; private set; } = false;

        private ScoreController()
        {
            Score = new ReactiveProperty<int>(0);
            HighScore = new ReactiveProperty<int>(0);
            disposables = new CompositeDisposable();

            Score
                .Where(score => score > HighScore.Value)
                .Subscribe(score => SetHighScore(score))
                .AddTo(disposables);
        }

        public async UniTask InitAsync()
        {
            saveData = await saveController.LoadAsync();
            HighScore.Value = saveData.HighScore;
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
        }
    }
}
