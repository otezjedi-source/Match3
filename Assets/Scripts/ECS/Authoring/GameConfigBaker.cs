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

            // Grid dimensions and generation settings
            AddComponent<GridConfig>(entity, new()
            {
                Width = authoring.gameConfig.GridWidth,
                Height = authoring.gameConfig.GridHeight,
                MaxInitAttempts = authoring.gameConfig.MaxGridInitAttempts,
            });

            // Matching rules and scoring
            AddComponent<MatchConfig>(entity, new()
            {
                MatchCount = authoring.gameConfig.MatchCount,
                PointsPerTile = authoring.gameConfig.PointsPerTile,
            });

            // Animation durations
            AddComponent<TimingConfig>(entity, new()
            {
                SwapDuration = authoring.gameConfig.SwapDuration,
                FallDuration = authoring.gameConfig.FallDuration,
                MatchDelay = authoring.gameConfig.MatchDelay,
            });

            // Tag for querying config entity
            AddComponent<GameConfigTag>(entity);
        }
    }
}
