using Match3.ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Match3.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(GameSystemGroup))]
    public partial struct GridCacheSystem : ISystem
    {
        [BurstCompile]
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<GridTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            var dirtyFlag = SystemAPI.GetComponentRW<GridDirtyFlag>(gridEntity);
            if (!dirtyFlag.ValueRO.IsDirty)
                return;

            var config = SystemAPI.GetSingleton<GridConfig>();
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);
            var typeCache = SystemAPI.GetBuffer<GridTileTypeCache>(gridEntity);
            var tileLookup = SystemAPI.GetComponentLookup<TileData>(true);

            if (typeCache.Length != config.CellCount)
                typeCache.Length = config.CellCount;

            var job = new UpdateTypeCacheJob
            {
                GridCells = gridCells.AsNativeArray(),
                TypeCache = typeCache.AsNativeArray(),
                TileLookup = tileLookup,
            };
            job.Schedule(gridCells.Length, 64).Complete();

            dirtyFlag.ValueRW.IsDirty = false;

            var movesCache = SystemAPI.GetSingletonRW<PossibleMovesCache>();
            movesCache.ValueRW.IsValid = false;
        }
    }

    [BurstCompile]
    struct UpdateTypeCacheJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<GridCell> GridCells;
        [WriteOnly] public NativeArray<GridTileTypeCache> TypeCache;
        [ReadOnly] public ComponentLookup<TileData> TileLookup;

        public void Execute(int i)
        {
            if (GridCells[i].IsEmpty)
                TypeCache[i] = new() { Type = TileType.None };
            else
            {
                var tileData = TileLookup[GridCells[i].Tile];
                TypeCache[i] = new() { Type = tileData.Type };
            }
        }
    }
}
