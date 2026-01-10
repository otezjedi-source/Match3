using Match3.ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(FallSystem))]
    public partial struct PossibleMovesSystem : ISystem
    {
        private NativeList<TileType> gridTypesCache;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<MatchConfig>();
            state.RequireForUpdate<PossibleMovesCache>();

            gridTypesCache = new(64, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (gridTypesCache.IsCreated)
                gridTypesCache.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameState>();
            if (gameState.ValueRO.Phase != GamePhase.Idle)
                return;

            var movesCache = SystemAPI.GetSingletonRW<PossibleMovesCache>();
            if (!movesCache.ValueRO.IsValid)
            {
                var gridConfig = SystemAPI.GetSingleton<GridConfig>();
                var matchConfig = SystemAPI.GetSingleton<MatchConfig>();
                var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
                var typeCache = SystemAPI.GetBuffer<GridTileTypeCache>(gridEntity).AsNativeArray();

                gridTypesCache.Clear();
                for (int i = 0; i < typeCache.Length; i++)
                    gridTypesCache.Add(typeCache[i].Type);

                bool hasMoves = PossibleMovesChecker.CheckMoves(gridTypesCache, ref gridConfig, ref matchConfig);
                movesCache.ValueRW.HasMoves = hasMoves;
                movesCache.ValueRW.IsValid = true;
            }

            if (!movesCache.ValueRO.HasMoves)
            {
                gameState.ValueRW.Phase = GamePhase.GameOver;
                state.EntityManager.CreateSingleton<GameOverEvent>();
            }
        }
    }
    
    [BurstCompile]
    public static class PossibleMovesChecker
    {
        [BurstCompile]
        public static bool CheckMoves(NativeList<TileType> types, ref GridConfig gridConfig, ref MatchConfig matchConfig)
        {
            for (int x = 0; x < gridConfig.Width; x++)
            {
                for (int y = 0; y < gridConfig.Height; y++)
                {
                    if (x < gridConfig.Width - 1 && TrySwapCheck(types, x, y, x + 1, y, ref gridConfig, ref matchConfig))
                        return true;

                    if (y < gridConfig.Height - 1 && TrySwapCheck(types, x, y, x, y + 1, ref gridConfig, ref matchConfig))
                        return true;
                }
            }
            return false;
        }

        [BurstCompile]
        private static bool TrySwapCheck(
            NativeList<TileType> types,
            int x1, int y1, int x2, int y2,
            ref GridConfig gridConfig, ref MatchConfig matchConfig)
        {
            int idx1 = gridConfig.GetIndex(x1, y1);
            int idx2 = gridConfig.GetIndex(x2, y2);

            var typeA = types[idx1];
            var typeB = types[idx2];

            if (typeA == TileType.None || typeB == TileType.None)
                return false;

            types[idx1] = typeB;
            types[idx2] = typeA;

            bool hasMatch = HasMatchAt(types, x1, y1, ref gridConfig, ref matchConfig) ||
                           HasMatchAt(types, x2, y2, ref gridConfig, ref matchConfig);

            types[idx1] = typeA;
            types[idx2] = typeB;

            return hasMatch;
        }

        [BurstCompile]
        private static bool HasMatchAt(
            NativeList<TileType> types,
            int x, int y,
            ref GridConfig gridConfig, ref MatchConfig matchConfig)
        {
            var type = types[gridConfig.GetIndex(x, y)];
            if (type == TileType.None)
                return false;

            int hCount = 1;
            for (int i = x - 1; i >= 0 && types[gridConfig.GetIndex(i, y)] == type; i--)
                hCount++;
            for (int i = x + 1; i < gridConfig.Width && types[gridConfig.GetIndex(i, y)] == type; i++)
                hCount++;

            if (hCount >= matchConfig.MatchCount)
                return true;

            int vCount = 1;
            for (int i = y - 1; i >= 0 && types[gridConfig.GetIndex(x, i)] == type; i--)
                vCount++;
            for (int i = y + 1; i < gridConfig.Height && types[gridConfig.GetIndex(x, i)] == type; i++)
                vCount++;

            return vCount >= matchConfig.MatchCount;
        }
    }
}
