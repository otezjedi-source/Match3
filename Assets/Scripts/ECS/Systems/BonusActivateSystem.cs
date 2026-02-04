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

                ApplyBonus(bonusData.ValueRO.type, tileData.ValueRO.gridPos, gridConfig);
                bonusData.ValueRW.type = BonusType.None;
            }

            if (affectedPositions.Count > 0)
                MarkTiles(ref state, gridCells, gridConfig);
        }

        private void ApplyBonus(BonusType bonusType, int2 bonusPos, GridConfig gridConfig)
        {
            switch (bonusType)
            {
                case BonusType.LineHorizontal:
                    AddRow(bonusPos.y, gridConfig);
                    break;
                
                case BonusType.LineVertical:
                    AddColumn(bonusPos.x, gridConfig);
                    break;
                
                case BonusType.Bomb:
                    AddArea(bonusPos, 1, gridConfig);
                    break;
                
                case BonusType.Cross:
                    AddColumn(bonusPos.x, gridConfig);
                    AddRow(bonusPos.y, gridConfig);
                    break;
                    
                case BonusType.BombHorizontal:
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (bonusPos.y + dy >= 0 && bonusPos.y + dy < gridConfig.height)
                            AddRow(bonusPos.y + dy, gridConfig);
                    }
                    break;
                
                case BonusType.BombVertical:
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (bonusPos.x + dx >= 0 && bonusPos.x + dx < gridConfig.width)
                            AddColumn(bonusPos.x + dx, gridConfig);
                    }
                    break;
                
                case BonusType.BigBomb:
                    AddArea(bonusPos, 2, gridConfig);
                    break;
            }
        }
        
        private void AddRow(int y, GridConfig gridConfig)
        {
            for (int x = 0; x < gridConfig.width; x++)
                affectedPositions.Add(new int2(x, y));
        }

        private void AddColumn(int x, GridConfig gridConfig)
        {
            for (int y = 0; y < gridConfig.height; y++)
                affectedPositions.Add(new int2(x, y));
        }

        private void AddArea(int2 bonusPos, int radius, GridConfig gridConfig)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var pos = bonusPos + new int2(dx, dy);
                    if (gridConfig.IsValidPos(pos))
                        affectedPositions.Add(pos);
                }                
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