using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Save;
using UniRx;
using UnityEngine;
using VContainer;

namespace Match3.Controllers
{
    /// <summary>
    /// Manages current score and high score persistence.
    /// Exposes reactive properties for UI binding.
    /// </summary>
    public class ScoreController : IDisposable
    {
        [Inject] private readonly ISaveController saveController;

        public readonly ReactiveProperty<int> Score = new(0);
        public readonly ReactiveProperty<int> HighScore = new(0);
        public readonly ReactiveProperty<bool> IsReady = new(false);

        private readonly CompositeDisposable disposables = new();
        private readonly CancellationTokenSource cts = new();
        private CancellationTokenSource saveCts;
        private SaveData saveData;
        private bool isDisposed;

        /// <summary>
        /// True if current score exceeds previous high score.
        /// </summary>
        public bool IsNewHighScore { get; private set; } = false;

        #region Init/dispose
        public ScoreController()
        {
            // Auto-update high score when current score exceeds it
            Score
                .Where(_ => !isDisposed)
                .Where(score => score > HighScore.Value)
                .Subscribe(score => SetHighScore(score))
                .AddTo(disposables);
        }

        /// <summary>
        /// Load saved high score. Must be called before gameplay.
        /// </summary>
        public async UniTask InitAsync()
        {
            if (isDisposed)
                throw new ObjectDisposedException("[ScoreController] Trying to init disposed");

            try
            {
                saveData = await saveController.LoadAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScoreController] Failed to load save data: {ex.Message}");
                saveData = new();
            }

            HighScore.Value = saveData.HighScore;
            IsReady.Value = true;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            cts.Cancel();
            saveCts?.Cancel();
            saveCts?.Dispose();
            saveCts = null;
            cts.Dispose();

            // Final attempt to save on dispose
            if (saveData != null)
            {
                saveData.HighScore = HighScore.Value;
                saveController.SaveAsync(saveData).Forget();
            }

            disposables?.Dispose();
            Score?.Dispose();
            HighScore?.Dispose();
            IsReady?.Dispose();
            
        }
        #endregion

        #region Score
        /// <summary>
        /// Add points to current score. Called by ScoreSyncSystem.
        /// </summary>
        public void AddScore(int points)
        {
            if (isDisposed)
                return;

            if (points > 0)
                Score.Value += points;
            else
                Debug.LogWarning($"[ScoreController] Attempted to add {points} points");
        }

        /// <summary>
        /// Reset score for new game.
        /// </summary>
        public void ResetScore()
        {
            if (isDisposed)
                return;

            Score.Value = 0;
            IsNewHighScore = false;
        }
        #endregion

        #region High score
        private void SetHighScore(int newHighScore)
        {
            if (isDisposed)
                return;

            HighScore.Value = newHighScore;
            IsNewHighScore = true;
            ScheduleSave();
        }

        /// <summary>
        /// Reset high score to zero.
        /// </summary>
        public void ResetHighScore()
        {
            if (isDisposed)
                return;

            HighScore.Value = 0;
            IsNewHighScore = false;
            ScheduleSave();
        }
        #endregion

        #region Save
        private void ScheduleSave()
        {
            saveCts?.Cancel();
            saveCts?.Dispose();
            saveCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            SaveAsync(saveCts.Token).Forget();
        }
        
        private async UniTask SaveAsync(CancellationToken ct)
        {
            if (isDisposed || saveData == null)
                return;

            try
            {
                // Small delay to batch rapid score changes
                await UniTask.Delay(100, cancellationToken: ct);
                if (isDisposed)
                    return;

                saveData.HighScore = HighScore.Value;
                await saveController.SaveAsync(saveData);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[ScoreController] Failed to save: {ex.Message}");
            }
        }
        #endregion
    }
}
