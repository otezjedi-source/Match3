using Unity.Entities;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Initialization systems. Run once at startup.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GameInitSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }
    }
    
    /// <summary>
    /// Main game logic systems.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GameSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }
    }

    /// <summary>
    /// Sync systems bridge ECS to MonoBehaviour (views, audio, score).
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class GameSyncSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }
    }
}
