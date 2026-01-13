using Match3.ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace Match3.ECS.Authoring
{
    public class GameConfigBaker : Baker<GameConfigAuthoring>
    {
        public override void Bake(GameConfigAuthoring authoring)
        {
            if (!authoring.gameConfig.Validate(out var error))
            {
                Debug.LogError($"[GameConfigBaker] Invalid GameConfig: {error}");
                return;
            }

            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent<GridConfig>(entity, new()
            {
                Width = authoring.gameConfig.GridWidth,
                Height = authoring.gameConfig.GridHeight,
                MaxInitAttempts = authoring.gameConfig.MaxGridInitAttempts,
            });

            AddComponent<MatchConfig>(entity, new()
            {
                MatchCount = authoring.gameConfig.MatchCount,
                PointsPerTile = authoring.gameConfig.PointsPerTile,
            });

            AddComponent<TimingConfig>(entity, new()
            {
                SwapDuration = authoring.gameConfig.SwapDuration,
                FallDuration = authoring.gameConfig.FallDuration,
                MatchDelay = authoring.gameConfig.MatchDelay,
            });

            AddComponent<GameConfigTag>(entity);
        }
    }
}
