using Match3.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(BonusDetectSystem))]
    public partial struct BonusActivateSystem : ISystem
    {
        private NativeHashSet<int2> affectedPositions;
        private NativeList<Entity> markTiles;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<GameState>();
            
            affectedPositions = new(32, Allocator.Persistent);
            markTiles = new(32, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (affectedPositions.IsCreated)
                affectedPositions.Dispose();
            if (markTiles.IsCreated)
                markTiles.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameState>();
            if (gameState.phase != GamePhase.Clear)
                return;
            
            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();
            
            affectedPositions.Clear();

            foreach (var (bonusData, tileData) in
                     SystemAPI.Query<RefRW<TileBonusData>, RefRO<TileData>>()
                         .WithAll<MatchTag>()
                         .WithNone<ClearTag>())
            {
                if (bonusData.ValueRO.type == BonusType.None)
                    continue;

                RefreshAffectedPositions(bonusData.ValueRO.type, tileData.ValueRO.gridPos, gridConfig);
                bonusData.ValueRW.type = BonusType.None;
            }

            if (affectedPositions.Count > 0)
                MarkTiles(ref state, gridCells, gridConfig);
        }

        private void RefreshAffectedPositions(BonusType bonusType, int2 bonusPos, GridConfig gridConfig)
        {
            switch (bonusType)
            {
                case BonusType.LineHorizontal:
                    for (int x = 0; x < gridConfig.width; x++)
                        affectedPositions.Add(new(x, bonusPos.y));
                    break;
                
                case BonusType.LineVertical:
                    for (int y = 0; y < gridConfig.height; y++)
                        affectedPositions.Add(new(bonusPos.x, y));
                    break;
                
                case BonusType.Bomb:
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            var pos = bonusPos + new int2(dx, dy);
                            if (gridConfig.IsValidPos(pos))
                                affectedPositions.Add(pos);
                        }
                    }
                    break;
            }
        }

        private void MarkTiles(ref SystemState state, DynamicBuffer<GridCell> gridCells, GridConfig gridConfig)
        {
            markTiles.Clear();

            foreach (var pos in affectedPositions)
            {
                int idx = gridConfig.GetIndex(pos);
                if (gridCells[idx].IsEmpty)
                    continue;
                
                var tile = gridCells[idx].tile;
                if (SystemAPI.HasComponent<MatchTag>(tile))
                    continue;
                
                markTiles.Add(tile);
            }

            if (markTiles.Length > 0)
                state.EntityManager.AddComponent<MatchTag>(markTiles.AsArray());
        }
    }
}