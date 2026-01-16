using System;
using UniRx;
using UnityEngine;

namespace Match3.Controllers
{
    /// <summary>
    /// Tracks active loading operations. UI binds to IsLoading to show/hide loading screen.
    /// Uses reference counting: multiple operations can run concurrently.
    /// </summary>
    public class LoadingController : IDisposable
    {
        public IReadOnlyReactiveProperty<bool> IsLoading { get; }

        private readonly ReactiveProperty<int> activeOperations = new(0);
        private readonly CompositeDisposable disposables = new();
        private bool isDisposed;

        public LoadingController()
        {
            // Convert operation count to boolean for UI binding
            IsLoading = activeOperations
                .Select(count => count > 0)
                .ToReactiveProperty();

            if (IsLoading is IDisposable disposable)
                disposable.AddTo(disposables);
        }

        /// <summary>
        /// Start a loading operation. Returns IDisposable - dispose when operation completes.
        /// Usage: using (loadingController.BeginLoading()) { await SomeAsyncOp(); }
        /// </summary>
        public IDisposable BeginLoading()
        {
            if (isDisposed)
                throw new ObjectDisposedException($"[LoadingController] Trying to use disposed");

            activeOperations.Value++;
            return Disposable.Create(() =>
            {
                if (!isDisposed)
                    activeOperations.Value = Mathf.Max(0, activeOperations.Value - 1);
            });
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            disposables?.Dispose();
            activeOperations?.Dispose();
        }
    }
}
