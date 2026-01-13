using System;
using System.Threading;
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
        private CancellationTokenSource saveCts;
        private SaveData saveData;
        private bool isDisposed;

        public bool IsNewHighScore { get; private set; } = false;

        public ScoreController()
        {
            Score
                .Where(_ => !isDisposed)
                .Where(score => score > HighScore.Value)
                .Subscribe(score => SetHighScore(score))
                .AddTo(disposables);
        }

        public async UniTask InitAsync()
        {
            if (isDisposed)
                throw new ObjectDisposedException("[ScoreController] Trying to init disposed");

            try
            {
                saveData = await saveController.LoadAsync();
                HighScore.Value = saveData.HighScore;
                IsReady.Value = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScoreController] Failed to load save data: {ex.Message}");
                saveData = new();
                IsReady.Value = true;
            }
        }

        public void AddScore(int points)
        {
            if (isDisposed)
                return;

            if (points < 0)
            {
                Debug.LogWarning($"[ScoreController] Attempted to add {points} points");
                return;
            }
            
            Score.Value += points;
        }

        public void ResetScore()
        {
            if (isDisposed)
                return;

            Score.Value = 0;
            IsNewHighScore = false;
        }

        private void SetHighScore(int newHighScore)
        {
            if (isDisposed)
                return;

            HighScore.Value = newHighScore;

            if (saveData != null)
            {
                saveData.HighScore = newHighScore;
                IsNewHighScore = true;

                CancelPendingSave();
                saveCts = new();
                SaveAsync(saveCts.Token).Forget(ex =>
                {
                    if (ex is not OperationCanceledException)
                        Debug.LogError($"[ScoreController] Failed to save when setting high score: {ex.Message}");
                });
            }
        }

        public void ResetHighScore()
        {
            if (isDisposed)
                return;

            SetHighScore(0);
            IsNewHighScore = false;
        }

        private async UniTask SaveAsync(CancellationToken ct)
        {
            if (isDisposed || saveData == null)
                return;

            try
            {
                await UniTask.Delay(100, cancellationToken: ct);
                if (isDisposed || ct.IsCancellationRequested)
                    return;

                await saveController.SaveAsync(saveData);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save: {ex.Message}");
            }
        }

        private void CancelPendingSave()
        {
            if (saveCts == null)
                return;

            try
            {
                if (!saveCts.IsCancellationRequested)
                    saveCts.Cancel();
                saveCts.Dispose();
            }
            catch (OperationCanceledException) { }
            finally
            {
                saveCts = null;
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            CancelPendingSave();
            if (saveData != null && IsNewHighScore)
                saveController.SaveAsync(saveData).Forget(ex => Debug.LogError($"[ScoreController] Failed to save on dispose: {ex}"));

            disposables?.Dispose();
            Score?.Dispose();
            HighScore?.Dispose();
            IsReady?.Dispose();
        }
    }
}
