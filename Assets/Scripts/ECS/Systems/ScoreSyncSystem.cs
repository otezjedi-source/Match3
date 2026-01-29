using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Processes ScoreEvent entities and updates ScoreController.
    /// </summary>
    [UpdateInGroup(typeof(GameSyncSystemGroup))]
    public partial struct ScoreSyncSystem : ISystem
    {
        private EntityQuery scoreEventQuery;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            scoreEventQuery = SystemAPI.QueryBuilder().WithAll<ScoreEvent>().Build();
            state.RequireForUpdate(scoreEventQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs.scoreController == null)
                return;

            int points = 0;
            foreach (var scoreEvent in SystemAPI.Query<RefRO<ScoreEvent>>())
                points += scoreEvent.ValueRO.points;
            refs.scoreController.AddScore(points);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            ecb.DestroyEntity(scoreEventQuery, EntityQueryCaptureMode.AtPlayback);
        }
    }
}
