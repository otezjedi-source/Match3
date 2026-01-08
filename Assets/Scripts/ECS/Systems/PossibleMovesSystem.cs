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
        [BurstCompile]
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<MatchConfig>();
            state.RequireForUpdate<PossibleMovesCache>();
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

                bool hasMoves = PossibleMovesChecker.HasPossibleMoves(ref typeCache, ref gridConfig, ref matchConfig);
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
        public static bool HasPossibleMoves(ref NativeArray<GridTileTypeCache> typeCache, ref GridConfig gridConfig, ref MatchConfig matchConfig)
        {
            var types = new NativeArray<TileType>(typeCache.Length, Allocator.Temp);
            for (int i = 0; i < typeCache.Length; i++)
                types[i] = typeCache[i].Type;

            bool result = CheckMoves(ref types, ref gridConfig, ref matchConfig);

            types.Dispose();
            return result;
        }

        [BurstCompile]
        private static bool CheckMoves(ref NativeArray<TileType> types, ref GridConfig gridConfig, ref MatchConfig matchConfig)
        {
            for (int x = 0; x < gridConfig.Width; x++)
            {
                for (int y = 0; y < gridConfig.Height; y++)
                {
                    if (x < gridConfig.Width - 1 && TrySwapCheck(ref types, x, y, x + 1, y, ref gridConfig, ref matchConfig))
                        return true;

                    if (y < gridConfig.Height - 1 && TrySwapCheck(ref types, x, y, x, y + 1, ref gridConfig, ref matchConfig))
                        return true;
                }
            }
            return false;
        }

        [BurstCompile]
        private static bool TrySwapCheck(
            ref NativeArray<TileType> types,
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

            bool hasMatch = HasMatchAt(ref types, x1, y1, ref gridConfig, ref matchConfig) ||
                           HasMatchAt(ref types, x2, y2, ref gridConfig, ref matchConfig);

            types[idx1] = typeA;
            types[idx2] = typeB;

            return hasMatch;
        }

        [BurstCompile]
        private static bool HasMatchAt(
            ref NativeArray<TileType> types,
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
