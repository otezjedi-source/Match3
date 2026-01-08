using Match3.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(ClearCompleteSystem))]
    public partial struct FallSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<TimingConfig>();
            state.RequireForUpdate<ManagedReferences>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var phase = SystemAPI.GetSingleton<GameState>().Phase;
            if (phase != GamePhase.Fall)
                return;

            var clearQuery = SystemAPI.QueryBuilder().WithAll<ClearTag>().Build();
            if (!clearQuery.IsEmpty)
                return;

            var movingQuery = SystemAPI.QueryBuilder().WithAll<TileMove>().Build();
            if (!movingQuery.IsEmpty)
                return;

            var dropQuery = SystemAPI.QueryBuilder().WithAll<DropTag>().Build();
            if (!dropQuery.IsEmpty)
                return;

            foreach (var tileState in SystemAPI.Query<RefRO<TileStateData>>())
            {
                if (tileState.ValueRO.State == TileState.Fall)
                    return;
            }

            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.TileFactory == null || refs?.TileTypeRegistry == null)
                return;

            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var timingConfig = SystemAPI.GetSingleton<TimingConfig>();
            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();

            int dropCount = DropTiles(ref state, gridEntity, gridConfig, timingConfig);

            var gameState = SystemAPI.GetSingletonRW<GameState>();
            gameState.ValueRW.Phase = dropCount > 0 ? GamePhase.Fill : GamePhase.Match;
        }

        private int DropTiles(
            ref SystemState state,
            Entity gridEntity,
            GridConfig gridConfig,
            TimingConfig timingConfig)
        {
            int count = 0;
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);

            for (int x = 0; x < gridConfig.Width; x++)
            {
                for (int y = 0; y < gridConfig.Height; y++)
                {
                    int idx = gridConfig.GetIndex(x, y);
                    if (!gridCells[idx].IsEmpty)
                        continue;

                    for (int yAbove = y + 1; yAbove < gridConfig.Height; yAbove++)
                    {
                        int idxAbove = gridConfig.GetIndex(x, yAbove);
                        var tileAbove = gridCells[idxAbove].Tile;
                        if (tileAbove == Entity.Null)
                            continue;

                        gridCells[idx] = new() { Tile = tileAbove };
                        gridCells[idxAbove] = new() { Tile = Entity.Null };

                        var tileData = state.EntityManager.GetComponentData<TileData>(tileAbove);
                        tileData.GridPos = new(x, y);
                        state.EntityManager.SetComponentData(tileAbove, tileData);

                        float distance = yAbove - y;
                        float duration = timingConfig.FallDuration * distance;
                        TileMoveHelper.Start(state.EntityManager, tileAbove, new float3(x, y, 0), duration, TileState.Fall);
                        break;
                    }

                    ++count;
                }
            }
            return count;
        }
    }
}
