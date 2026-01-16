using Match3.UI;
using VContainer;
using VContainer.Unity;

namespace Match3.Core
{
    /// <summary>
    /// DI container for the start/menu scene. Child of BootLifetimeScope.
    /// Minimal setup - just registers the menu UI and entry point.
    /// </summary>
    public class StartLifetimeScope : LifetimeScope {
        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterComponentInHierarchy<MenuStart>();
            builder.RegisterEntryPoint<StartInitializer>();
        }
    }
}
