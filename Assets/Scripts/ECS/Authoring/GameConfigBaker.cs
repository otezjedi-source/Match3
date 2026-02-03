using Match3.ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace Match3.ECS.Authoring
{
    /// <summary>
    /// Baker that converts GameConfig ScriptableObject into ECS components.
    /// Splits config into separate components for better query performance.
    /// </summary>
    public class GameConfigBaker : Baker<GameConfigAuthoring>
    {
        public override void Bake(GameConfigAuthoring authoring)
        {
            // Validate config before baking
            if (!authoring.gameConfig.Validate(out var error))
            {
                Debug.LogError($"[GameConfigBaker] Invalid GameConfig: {error}");
                return;
            }

            var entity = GetEntity(TransformUsageFlags.None);
            
            // Tag for querying config entity
            AddComponent<ConfigTag>(entity);

            // Grid dimensions and generation settings
            AddComponent<GridConfig>(entity, new()
            {
                width = authoring.gameConfig.gridWidth,
                height = authoring.gameConfig.gridHeight,
                maxInitAttempts = authoring.gameConfig.maxGridInitAttempts,
            });

            // Matching rules and scoring
            AddComponent<MatchConfig>(entity, new()
            {
                matchCount = authoring.gameConfig.matchCount,
                pointsPerTile = authoring.gameConfig.pointsPerTile,
            });

            // Bonuses creation rules
            var buffer = AddBuffer<BonusConfig>(entity);
            buffer.EnsureCapacity(authoring.gameConfig.bonusesData.Count);
            foreach (var bonus in authoring.gameConfig.bonusesData)
            {
                buffer.Add(new()
                {
                    type = bonus.type,
                    matchCount = bonus.matchCount
                });
            }

            // Animation durations
            AddComponent<TimingConfig>(entity, new()
            {
                swapDuration = authoring.gameConfig.swapDuration,
                fallDuration = authoring.gameConfig.fallDuration,
                matchDelay = authoring.gameConfig.matchDelay,
            });
        }
    }
}
