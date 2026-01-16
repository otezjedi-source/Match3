using Match3.ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Checks if any valid moves exist. Triggers game over if no moves are possible.
    /// Runs during Idle phase after all animations complete.
    /// </summary>
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

            // Only recalculate when grid has changed (dirty flag cleared = cache invalid)
            var movesCache = SystemAPI.GetSingletonRW<PossibleMovesCache>();
            if (!movesCache.ValueRO.IsValid)
            {
                var gridConfig = SystemAPI.GetSingleton<GridConfig>();
                var matchConfig = SystemAPI.GetSingleton<MatchConfig>();
                var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
                var typeCache = SystemAPI.GetBuffer<GridTileTypeCache>(gridEntity);

                // Copy types to native list for move checking
                gridTypesCache.Clear();
                for (int i = 0; i < typeCache.Length; i++)
                    gridTypesCache.Add(typeCache[i].Type);

                bool hasMoves = PossibleMovesChecker.CheckMoves(ref gridTypesCache, ref gridConfig, ref matchConfig);
                movesCache.ValueRW.HasMoves = hasMoves;
                movesCache.ValueRW.IsValid = true;
            }

            // No moves = game over
            if (!movesCache.ValueRO.HasMoves)
            {
                gameState.ValueRW.Phase = GamePhase.GameOver;
                state.EntityManager.CreateSingleton<GameOverEvent>();
            }
        }
    }
    
    /// <summary>
    /// Static helper for checking if any valid swap exists.
    /// Used both by PossibleMovesSystem and GridTilesInitSystem.
    /// </summary>
    [BurstCompile]
    public static class PossibleMovesChecker
    {
        /// <summary>
        /// Brute force check: try every possible swap and see if it creates a match.
        /// O(width * height * 4) complexity, but grid is small so it's fine.
        /// </summary>
        [BurstCompile]
        public static bool CheckMoves(ref NativeList<TileType> types, ref GridConfig gridConfig, ref MatchConfig matchConfig)
        {
            for (int x = 0; x < gridConfig.Width; x++)
            {
                for (int y = 0; y < gridConfig.Height; y++)
                {
                    // Try swap right
                    if (x < gridConfig.Width - 1 && TrySwapCheck(ref types, x, y, x + 1, y, ref gridConfig, ref matchConfig))
                        return true;

                    // Try swap up
                    if (y < gridConfig.Height - 1 && TrySwapCheck(ref types, x, y, x, y + 1, ref gridConfig, ref matchConfig))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Temporarily swap two tiles, check for match, then swap back.
        /// </summary>
        [BurstCompile]
        private static bool TrySwapCheck(
            ref NativeList<TileType> types,
            int x1, int y1, int x2, int y2,
            ref GridConfig gridConfig, ref MatchConfig matchConfig)
        {
            int idx1 = gridConfig.GetIndex(x1, y1);
            int idx2 = gridConfig.GetIndex(x2, y2);

            var typeA = types[idx1];
            var typeB = types[idx2];

            if (typeA == TileType.None || typeB == TileType.None)
                return false;

            // Swap
            types[idx1] = typeB;
            types[idx2] = typeA;

            // Check both positions for matches
            bool hasMatch = HasMatchAt(ref types, x1, y1, ref gridConfig, ref matchConfig) ||
                           HasMatchAt(ref types, x2, y2, ref gridConfig, ref matchConfig);

            // Swap back
            types[idx1] = typeA;
            types[idx2] = typeB;

            return hasMatch;
        }

        /// <summary>
        /// Check if position is part of a match (horizontally or vertically).
        /// </summary>
        [BurstCompile]
        private static bool HasMatchAt(
            ref NativeList<TileType> types,
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
