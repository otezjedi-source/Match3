using Match3.ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Animates tile movement. Parallel job for performance.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(GridCacheSystem))]
    public partial struct TileMoveSystem : ISystem
    {
        [BurstCompile]
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TileMove>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            state.Dependency = new TileMoveJob { DeltaTime = deltaTime }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct TileMoveJob : IJobEntity
    {
        public float DeltaTime;

        public readonly void Execute(
            ref TileMove move,
            ref TileWorldPos worldPos,
            EnabledRefRW<TileMove> moveEnabled)
        {
            move.elapsed += DeltaTime;

            float t = math.saturate(move.elapsed / move.duration);
            float ease = t * t; // Ease-in quadratic
            worldPos.pos = math.lerp(move.startPos, move.targetPos, ease);

            if (t >= 1f)
            {
                worldPos.pos = move.targetPos;
                moveEnabled.ValueRW = false;
            }
        }
    }
    
    /// <summary>
    /// Helper for starting tile movement animation.
    /// </summary>
    public static class TileMoveHelper
    {
        public static void Start(
            EntityManager entityManager, 
            Entity entity, 
            float3 target, 
            float duration, 
            TileState tileState)
        {
            var pos = entityManager.GetComponentData<TileWorldPos>(entity).pos;
            entityManager.SetComponentData<TileStateData>(entity, new() { state = tileState });
            entityManager.SetComponentData<TileMove>(entity, new()
            {
                startPos = pos,
                targetPos = target,
                duration = duration,
                elapsed = 0
            });
            entityManager.SetComponentEnabled<TileMove>(entity, true);
        }
    }
}
