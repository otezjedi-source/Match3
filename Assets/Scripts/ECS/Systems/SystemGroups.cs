using Unity.Entities;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GameInitSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }
    }
    
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GameSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }
    }

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
