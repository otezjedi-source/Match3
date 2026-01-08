using System;
using Match3.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Random = UnityEngine.Random;

namespace Match3.ECS.Systems
{
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

            var config = SystemAPI.GetSingleton<GridConfig>();
            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();

            var cells = state.EntityManager.GetBuffer<GridCell>(gridEntity);
            cells.Length = config.CellCount;
            for (int i = 0; i < cells.Length; i++)
                cells[i] = new() { Tile = Entity.Null };

            var typeCache = state.EntityManager.GetBuffer<GridTileTypeCache>(gridEntity);
            typeCache.Length = config.CellCount;
            for (int i = 0; i < typeCache.Length; i++)
                typeCache[i] = new() { Type = TileType.None };
        }
    }

    [UpdateInGroup(typeof(GameInitSystemGroup))]
    [UpdateAfter(typeof(GridBufferInitSystem))]
    public partial struct GridTilesInitSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<MatchConfig>();
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<GridStartRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.TileFactory == null || refs?.TileTypeRegistry == null)
                return;

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var matchConfig = SystemAPI.GetSingleton<MatchConfig>();
            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            var typeCache = SystemAPI.GetBuffer<GridTileTypeCache>(gridEntity).AsNativeArray();

            var types = new NativeArray<TileType>(gridConfig.CellCount, Allocator.Temp);
            GenerateTypes(types, refs.TileTypeRegistry.All, gridConfig);
            for (int i = 0; i < types.Length; i++)
                typeCache[i] = new() { Type = types[i] };
            
            int attempts = 0;
            while (attempts < 100)
            {
                if (PossibleMovesChecker.HasPossibleMoves(ref typeCache, ref gridConfig, ref matchConfig))
                    break;

                GenerateTypes(types, refs.TileTypeRegistry.All, gridConfig);
                for (int i = 0; i < types.Length; i++)
                    typeCache[i] = new() { Type = types[i] };
                ++attempts;
            }

            var tiles = new Entity[gridConfig.CellCount];
            for (int y = 0; y < gridConfig.Height; y++)
            {
                for (int x = 0; x < gridConfig.Width; x++)
                {
                    int idx = gridConfig.GetIndex(x, y);
                    tiles[idx] = refs.TileFactory.Create(x, y, types[idx]);
                }
            }

            types.Dispose();

            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);
            for (int i = 0; i < gridCells.Length; i++)
                gridCells[i] = new() { Tile = tiles[i] };

            var dirtyFlag = SystemAPI.GetComponentRW<GridDirtyFlag>(gridEntity);
            dirtyFlag.ValueRW.IsDirty = true;

            var movesCache = SystemAPI.GetComponentRW<PossibleMovesCache>(gridEntity);
            movesCache.ValueRW.IsValid = true;

            var request = SystemAPI.QueryBuilder().WithAll<GridStartRequest>().Build();
            state.EntityManager.DestroyEntity(request);
        }

        private readonly void GenerateTypes(NativeArray<TileType> result, ReadOnlySpan<TileType> types, GridConfig config)
        {
            var allowed = new NativeList<TileType>(types.Length, Allocator.Temp);

            for (int y = 0; y < config.Height; y++)
            {
                for (int x = 0; x < config.Width; x++)
                {
                    allowed.Clear();

                    foreach (var t in types)
                    {
                        if (x >= 2 && t == result[config.GetIndex(x - 1, y)] && t == result[config.GetIndex(x - 2, y)])
                            continue;
                        if (y >= 2 && t == result[config.GetIndex(x, y - 1)] && t == result[config.GetIndex(x, y - 2)])
                            continue;

                        allowed.Add(t);
                    }

                    int idx = config.GetIndex(x, y);
                    int rnd = Random.Range(0, allowed.Length);
                    result[idx] = allowed[rnd];
                }
            }

            allowed.Dispose();
        }
    }
}
