using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Authoring
{
    public class GameConfigBaker : Baker<GameConfigAuthoring>
    {
        public override void Bake(GameConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent<GridConfig>(entity, new()
            {
                Width = authoring.config.GridWidth,
                Height = authoring.config.GridHeight,
                MaxInitAttempts = authoring.config.MaxGridInitAttempts,
            });

            AddComponent<MatchConfig>(entity, new()
            {
                MatchCount = authoring.config.MatchCount,
                PointsPerTile = authoring.config.PointsPerTile,
            });

            AddComponent<TimingConfig>(entity, new()
            {
                SwapDuration = authoring.config.SwapDuration,
                FallDuration = authoring.config.FallDuration,
                MatchDelay = authoring.config.MatchDelay,
            });

            AddComponent<GameConfigTag>(entity);
        }
    }
}
