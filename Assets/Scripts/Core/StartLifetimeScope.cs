using MiniIT.GAME;
using MiniIT.UI;
using VContainer;
using VContainer.Unity;

namespace MiniIT.CORE
{
    public class StartLifetimeScope : LifetimeScope {
        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterComponentInHierarchy<MenuStart>();
            builder.RegisterEntryPoint<StartInitializer>();
        }
    }
}
