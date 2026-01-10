using System;
using UniRx;

namespace Match3.Controllers
{
    public class LoadingController
    {
        public IReadOnlyReactiveProperty<bool> IsLoading { get; }

        private readonly ReactiveProperty<int> activeOperations = new(0);

        public LoadingController()
        {
            IsLoading = activeOperations
                .Select(count => count > 0)
                .ToReactiveProperty();
        }

        public IDisposable BeginLoading()
        {
            activeOperations.Value++;
            return Disposable.Create(() => activeOperations.Value--);
        }
    }
}
