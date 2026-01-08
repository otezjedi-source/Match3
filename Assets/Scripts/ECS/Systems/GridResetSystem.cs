using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(GameInitSystemGroup))]
    [UpdateBefore(typeof(GridTilesInitSystem))]
    public partial struct GridResetSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<GridResetRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs?.TileFactory == null)
                return;

            var gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);

            for (int i = 0; i < gridCells.Length; i++)
            {
                if (!gridCells[i].IsEmpty)
                {
                    refs.TileFactory.Return(gridCells[i].Tile);
                    gridCells[i] = new() { Tile = Entity.Null };
                }
            }

            var gameState = SystemAPI.GetSingletonRW<GameState>();
            gameState.ValueRW.Phase = GamePhase.Idle;
            gameState.ValueRW.PhaseTimer = 0;

            var dirtyFlag = SystemAPI.GetComponentRW<GridDirtyFlag>(gridEntity);
            dirtyFlag.ValueRW.IsDirty = true;

            var movesCache = SystemAPI.GetComponentRW<PossibleMovesCache>(gridEntity);
            movesCache.ValueRW.IsValid = false;

            var request = SystemAPI.QueryBuilder().WithAll<GridResetRequest>().Build();
            state.EntityManager.DestroyEntity(request);

            state.EntityManager.CreateSingleton<GridStartRequest>();
        }
    }
}
