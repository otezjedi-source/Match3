using System;
using UniRx;
using UnityEngine;

namespace Match3.Controllers
{
    public class LoadingController : IDisposable
    {
        public IReadOnlyReactiveProperty<bool> IsLoading { get; }

        private readonly ReactiveProperty<int> activeOperations = new(0);
        private readonly CompositeDisposable disposables = new();
        private bool isDisposed;

        public LoadingController()
        {
            IsLoading = activeOperations
                .Select(count => count > 0)
                .ToReactiveProperty();

            if (IsLoading is IDisposable disposable)
                disposable.AddTo(disposables);
        }

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
