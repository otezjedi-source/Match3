using System;
using Match3.ECS.Components;
using Unity.Collections;
using Unity.Entities;
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
            state.RequireForUpdate<ConfigTag>();
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
            state.EntityManager.AddComponentData<GameState>(entity, new()
            {
                phase = GamePhase.Idle,
                phaseTimer = 0,
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
            state.EntityManager.AddComponentData<GridDirtyFlag>(entity, new() { isDirty = true });
            state.EntityManager.AddComponentData<PossibleMovesCache>(entity, new() { isValid = false, hasMoves = true });
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

            var cells = SystemAPI.GetSingletonBuffer<GridCell>();
            cells.Length = gridConfig.CellCount;
            for (int i = 0; i < cells.Length; i++)
                cells[i] = new() { tile = Entity.Null };

            var typeCache = SystemAPI.GetSingletonBuffer<GridTileTypeCache>();
            typeCache.Length = gridConfig.CellCount;
            for (int i = 0; i < typeCache.Length; i++)
                typeCache[i] = new() { type = TileType.None };
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
        private EntityQuery requestQuery;
        private Random random;

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
            requestQuery = SystemAPI.QueryBuilder().WithAll<GridStartRequest>().Build();
            random = new((uint)Environment.TickCount);
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
            if (refs.tileFactory == null || refs.tileTypeRegistry == null)
                return;

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var matchConfig = SystemAPI.GetSingleton<MatchConfig>();
            var typeCache = SystemAPI.GetSingletonBuffer<GridTileTypeCache>();

            // Generate types, retry if no valid moves exist
            GenerateTypes(typeCache, refs.tileTypeRegistry.All, gridConfig, matchConfig);
            
            int attempts = 0;
            while (attempts < gridConfig.maxInitAttempts)
            {
                if (PossibleMovesChecker.CheckMoves(ref gridTypesCache, ref gridConfig, ref matchConfig))
                    break;

                GenerateTypes(typeCache, refs.tileTypeRegistry.All, gridConfig, matchConfig);
                ++attempts;
            }

            // Create actual tile entities from the generated types
            gridTilesCache.Clear();
            for (int y = 0; y < gridConfig.height; y++)
            {
                for (int x = 0; x < gridConfig.width; x++)
                {
                    int idx = gridConfig.GetIndex(x, y);
                    var tile = refs.tileFactory.Create(x, y, gridTypesCache[idx]);
                    gridTilesCache.Add(tile);
                }
            }

            // Store tile references in grid buffer
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();
            for (int i = 0; i < gridCells.Length; i++)
                gridCells[i] = new() { tile = gridTilesCache[i] };

            SystemAPI.GetSingletonRW<GridDirtyFlag>().ValueRW.isDirty = true;
            SystemAPI.GetSingletonRW<PossibleMovesCache>().ValueRW.isValid = true;

            var gameState = SystemAPI.GetSingletonRW<GameState>();
            gameState.ValueRW.phase = GamePhase.Idle;
            gameState.ValueRW.phaseTimer = 0;

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
            for (int y = 0; y < gridConfig.height; y++)
            {
                for (int x = 0; x < gridConfig.width; x++)
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
                    var type = allowedTypes.Length == 0 
                        ? types[random.NextInt(types.Length)] 
                        : allowedTypes[random.NextInt(allowedTypes.Length)];
                    typeCache[idx] = new() { type = type };
                }
            }

            // Copy to cache for move validation
            gridTypesCache.Clear();
            for (int i = 0; i < typeCache.Length; i++)
                gridTypesCache.Add(typeCache[i].type);
            return;

            bool CanMatch(int x, int y, TileType type, int dx, int dy)
            {
                for (int i = 1; i < matchConfig.matchCount; i++)
                {
                    int nx = x + dx * i;
                    int ny = y + dy * i;
                    if (!gridConfig.IsValidPos(nx, ny))
                        return false;

                    var prevType = typeCache[gridConfig.GetIndex(nx, ny)].type;
                    if (prevType != type)
                        return false;
                }

                return true;
            }
        }
    }
}
