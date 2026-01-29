using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Resets grid for new game. Returns all tiles to pool.
    /// </summary>
    [UpdateInGroup(typeof(GameInitSystemGroup))]
    [UpdateBefore(typeof(GridTilesInitSystem))]
    public partial struct GridResetSystem : ISystem
    {
        private EntityQuery requestQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<GridResetRequest>();
            
            requestQuery = SystemAPI.QueryBuilder().WithAll<GridResetRequest>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();
            if (refs.tileFactory == null)
                return;
            
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();

            // Return all tiles to pool
            for (int i = 0; i < gridCells.Length; i++)
            {
                if (gridCells[i].IsEmpty) 
                    continue;
                
                refs.tileFactory.Return(gridCells[i].tile);
                gridCells[i] = new() { tile = Entity.Null };
            }

            var gameState = SystemAPI.GetSingletonRW<GameState>();
            gameState.ValueRW.phase = GamePhase.Idle;
            gameState.ValueRW.phaseTimer = 0;

            SystemAPI.GetSingletonRW<GridDirtyFlag>().ValueRW.isDirty = true;
            SystemAPI.GetSingletonRW<PossibleMovesCache>().ValueRW.isValid = false;

            // Consume reset request and trigger regeneration
            state.EntityManager.DestroyEntity(requestQuery);
            state.EntityManager.CreateSingleton<GridStartRequest>();
        }
    }
}
