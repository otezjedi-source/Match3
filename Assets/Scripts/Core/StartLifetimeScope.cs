using Match3.Game;
using Match3.UI;
using VContainer;
using VContainer.Unity;

namespace Match3.Core
{
    public class StartLifetimeScope : LifetimeScope {
        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterComponentInHierarchy<MenuStart>();
            builder.RegisterEntryPoint<StartInitializer>();
        }
    }
}
