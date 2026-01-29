using Match3.ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Rebuilds the tile type cache when grid changes.
    /// The cache enables fast matching without repeated entity lookups.
    /// Uses parallel job for better performance on larger grids.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameSystemGroup))]
    public partial struct GridCacheSystem : ISystem
    {
        private ComponentLookup<TileData> tileLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<GridConfig>();
            
            tileLookup = SystemAPI.GetComponentLookup<TileData>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dirtyFlag = SystemAPI.GetSingletonRW<GridDirtyFlag>();
            if (!dirtyFlag.ValueRO.isDirty)
                return;
            
            tileLookup.Update(ref state);
            
            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();
            var typeCache = SystemAPI.GetSingletonBuffer<GridTileTypeCache>();

            if (typeCache.Length != gridConfig.CellCount)
                typeCache.Length = gridConfig.CellCount;

            var job = new UpdateTypeCacheJob
            {
                GridCells = gridCells.AsNativeArray(),
                TypeCache = typeCache.AsNativeArray(),
                TileLookup = tileLookup,
            };
            state.Dependency = job.Schedule(gridCells.Length, 64, state.Dependency);
            state.Dependency.Complete();

            dirtyFlag.ValueRW.isDirty = false;

            // Invalidate moves cache since grid changed
            SystemAPI.GetSingletonRW<PossibleMovesCache>().ValueRW.isValid = false;
        }
    }

    [BurstCompile]
    internal struct UpdateTypeCacheJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<GridCell> GridCells;
        [WriteOnly] public NativeArray<GridTileTypeCache> TypeCache;
        [ReadOnly] public ComponentLookup<TileData> TileLookup;

        public void Execute(int i)
        {
            if (GridCells[i].IsEmpty)
                TypeCache[i] = new() { type = TileType.None };
            else
            {
                var tileData = TileLookup[GridCells[i].tile];
                TypeCache[i] = new() { type = tileData.type };
            }
        }
    }
}
