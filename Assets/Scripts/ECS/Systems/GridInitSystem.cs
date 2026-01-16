using System;
using Match3.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Random = Unity.Mathematics.Random;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Creates the initial ECS singletons for game state and grid.
    /// Runs once at startup, then disables itself.
    /// </summary>
    [UpdateInGroup(typeof(GameInitSystemGroup), OrderFirst = true)]
    public partial struct GridInitSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameConfigTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            CreateGameState(ref state);
            CreateGrid(ref state);
        }

        private void CreateGameState(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GameStateTag>())
                return;

            var entity = state.EntityManager.CreateSingleton<GameStateTag>();
            state.EntityManager.AddComponentData(entity, new GameState
            {
                Phase = GamePhase.Idle,
                PhaseTimer = 0,
            });
        }

        private void CreateGrid(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GridTag>())
                return;

            var entity = state.EntityManager.CreateSingleton<GridTag>();
            state.EntityManager.AddBuffer<GridCell>(entity);
            state.EntityManager.AddBuffer<GridTileTypeCache>(entity);
            state.EntityManager.AddBuffer<MatchResult>(entity);
            state.EntityManager.AddComponentData(entity, new GridDirtyFlag { IsDirty = true });
            state.EntityManager.AddComponentData(entity, new PossibleMovesCache { IsValid = false, HasMoves = true });
        }
    }

    /// <summary>
    /// Allocates grid buffers based on config dimensions.
    /// </summary>
    [UpdateInGroup(typeof(GameInitSystemGroup))]
    [UpdateAfter(typeof(GridInitSystem))]
    public partial struct GridBufferInitSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<GridTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();

            var cells = state.EntityManager.GetBuffer<GridCell>(gridEntity);
            cells.Length = gridConfig.CellCount;
            for (int i = 0; i < cells.Length; i++)
                cells[i] = new() { Tile = Entity.Null };

            var typeCache = state.EntityManager.GetBuffer<GridTileTypeCache>(gridEntity);
            typeCache.Length = gridConfig.CellCount;
            for (int i = 0; i < typeCache.Length; i++)
                typeCache[i] = new() { Type = TileType.None };
        }
    }

    /// <summary>
    /// Generates initial tile layout when GridStartRequest is present.
    /// Ensures no matches exist on initial board and at least one valid move is available.
    /// </summary>
    [UpdateInGroup(typeof(GameInitSystemGroup))]
    [UpdateAfter(typeof(GridBufferInitSystem))]
    public partial struct GridTilesInitSystem : ISystem
    {
        private NativeList<TileType> allowedTypes;
        private NativeList<Entity> gridTilesCache;
        private NativeList<TileType> gridTypesCache;
        private Random random;
        private EntityQuery requestQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<MatchConfig>();
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<GridStartRequest>();
            state.RequireForUpdate<GameState>();

            allowedTypes = new(8, Allocator.Persistent);
            gridTilesCache = new(64, Allocator.Persistent);
            gridTypesCache = new(64, Allocator.Persistent);

            random = new((uint)Environment.TickCount);
            requestQuery = SystemAPI.QueryBuilder().WithAll<GridStartRequest>().Build();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (allowedTypes.IsCreated)
                allowedTypes.Dispose();
            if (gridTilesCache.IsCreated)
                gridTilesCache.Dispose();
            if (gridTypesCache.IsCreated)
                gridTypesCache.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.TileFactory == null || refs?.TileTypeRegistry == null)
                return;

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var matchConfig = SystemAPI.GetSingleton<MatchConfig>();
            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            var typeCache = SystemAPI.GetBuffer<GridTileTypeCache>(gridEntity);

            // Generate types, retry if no valid moves exist
            GenerateTypes(typeCache, refs.TileTypeRegistry.All, gridConfig, matchConfig);
            
            int attempts = 0;
            while (attempts < gridConfig.MaxInitAttempts)
            {
                if (PossibleMovesChecker.CheckMoves(ref gridTypesCache, ref gridConfig, ref matchConfig))
                    break;

                GenerateTypes(typeCache, refs.TileTypeRegistry.All, gridConfig, matchConfig);
                ++attempts;
            }

            // Create actual tile entities from the generated types
            gridTilesCache.Clear();
            for (int y = 0; y < gridConfig.Height; y++)
            {
                for (int x = 0; x < gridConfig.Width; x++)
                {
                    int idx = gridConfig.GetIndex(x, y);
                    var tile = refs.TileFactory.Create(x, y, gridTypesCache[idx]);
                    gridTilesCache.Add(tile);
                }
            }

            // Store tile references in grid buffer
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);
            for (int i = 0; i < gridCells.Length; i++)
                gridCells[i] = new() { Tile = gridTilesCache[i] };

            var dirtyFlag = SystemAPI.GetComponentRW<GridDirtyFlag>(gridEntity);
            dirtyFlag.ValueRW.IsDirty = true;

            var movesCache = SystemAPI.GetComponentRW<PossibleMovesCache>(gridEntity);
            movesCache.ValueRW.IsValid = true;

            var gameState = SystemAPI.GetSingletonRW<GameState>();
            gameState.ValueRW.Phase = GamePhase.Idle;
            gameState.ValueRW.PhaseTimer = 0;

            state.EntityManager.DestroyEntity(requestQuery);
        }

        /// <summary>
        /// Generate tile types ensuring no initial matches.
        /// For each cell, filters out types that would create a match with neighbors.
        /// </summary>
        private void GenerateTypes(
            DynamicBuffer<GridTileTypeCache> typeCache,
            ReadOnlySpan<TileType> types,
            GridConfig gridConfig, MatchConfig matchConfig)
        {
            for (int y = 0; y < gridConfig.Height; y++)
            {
                for (int x = 0; x < gridConfig.Width; x++)
                {
                    allowedTypes.Clear();

                    foreach (var t in types)
                    {
                        if (CanMatch(x, y, t, -1, 0))
                            continue;
                        if (CanMatch(x, y, t, 0, -1))
                            continue;

                        allowedTypes.Add(t);
                    }

                    int idx = gridConfig.GetIndex(x, y);
                    int rnd = random.NextInt(0, allowedTypes.Length);
                    typeCache[idx] = new() { Type = allowedTypes[rnd] };
                }
            }

            // Copy to cache for move validation
            gridTypesCache.Clear();
            for (int i = 0; i < typeCache.Length; i++)
                gridTypesCache.Add(typeCache[i].Type);

            bool CanMatch(int x, int y, TileType type, int dx, int dy)
            {
                for (int i = 1; i < matchConfig.MatchCount; i++)
                {
                    int nx = x + dx * i;
                    int ny = y + dy * i;
                    if (!gridConfig.IsValidPos(nx, ny))
                        return false;

                    var prevType = typeCache[gridConfig.GetIndex(nx, ny)].Type;
                    if (prevType != type)
                        return false;
                }

                return true;
            }
        }
    }
}
