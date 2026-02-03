using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Determines if a bonus should be created based on match count.
    /// Runs after MatchSystem finds matches and before ClearSystem destroys tiles.
    /// Creates CreateBonusRequest for BonusSpawnSystem to process.
    /// </summary>
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(MatchSystem))]
    public partial struct BonusDetectSystem : ISystem
    {
        private EntityQuery swapRequestQuery;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<ConfigTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            swapRequestQuery = SystemAPI.QueryBuilder().WithAll<SwapRequest>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameState>();
            if (gameState.phase != GamePhase.Clear)
                return;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            if (!swapRequestQuery.IsEmpty)
                ecb.DestroyEntity(swapRequestQuery, EntityQueryCaptureMode.AtPlayback);

            var matchGroups = SystemAPI.GetSingletonBuffer<MatchGroup>(true);
            if (matchGroups.Length == 0)
                return;
            
            var bonusConfig = SystemAPI.GetSingletonBuffer<BonusConfig>(true);
            if (bonusConfig.Length == 0)
                return;
            
            foreach (var group in matchGroups)
            {
                var bonusType = GetBonusType(group, bonusConfig);
                if (bonusType == BonusType.None)
                    continue;

                var requestEntity = ecb.CreateEntity();
                ecb.AddComponent<CreateBonusRequest>(requestEntity, new()
                {
                    type = bonusType,
                    pos = group.bonusPos
                });
            }
            
            SystemAPI.GetSingletonBuffer<MatchGroup>().Clear();
        }
        
        private BonusType GetBonusType(MatchGroup group, DynamicBuffer<BonusConfig> bonusConfig)
        {
            var result = BonusType.None;
            int maxMatchCount = 0;

            foreach (var config in bonusConfig)
            {
                if (group.count < config.matchCount || config.matchCount <= maxMatchCount)
                    continue;
                
                if (config.type == BonusType.LineHorizontal && !group.IsHorizontalLine)
                    continue;
                if (config.type == BonusType.LineVertical && !group.IsVerticalLine)
                    continue;

                result = config.type;
                maxMatchCount = config.matchCount;
            }

            return result;
        }
    }
}
