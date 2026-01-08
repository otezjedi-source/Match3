using Match3.ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Systems
{
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
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            new TileMoveJob { DeltaTime = deltaTime }.ScheduleParallel();
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
            move.Elapsed += DeltaTime;

            float t = math.saturate(move.Elapsed / move.Duration);
            float ease = t * t;
            worldPos.Pos = math.lerp(move.StartPos, move.TargetPos, ease);

            if (t >= 1f)
            {
                worldPos.Pos = move.TargetPos;
                moveEnabled.ValueRW = false;
            }
        }
    }
    
    public static class TileMoveHelper
    {
        public static void Start(EntityManager entityMgr, Entity entity, float3 target, float duration, TileState tileState)
        {
            var pos = entityMgr.GetComponentData<TileWorldPos>(entity).Pos;
            entityMgr.SetComponentData<TileStateData>(entity, new() { State = tileState });
            entityMgr.SetComponentData<TileMove>(entity, new()
            {
                StartPos = pos,
                TargetPos = target,
                Duration = duration,
                Elapsed = 0
            });
            entityMgr.SetComponentEnabled<TileMove>(entity, true);
        }
    }
}
