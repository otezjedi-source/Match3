using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    [UpdateInGroup(typeof(GameSystemGroup))]
    [UpdateAfter(typeof(FillSystem))]
    public partial struct BonusInitSystem : ISystem
    {
        private EntityQuery requestQuery;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            requestQuery = SystemAPI.QueryBuilder().WithAll<CreateBonusRequest>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameState>();
            if (gameState.phase != GamePhase.Idle)
                return;
            
            var gridConfig = SystemAPI.GetSingleton<GridConfig>();
            var gridCells = SystemAPI.GetSingletonBuffer<GridCell>();
            
            foreach (var request in SystemAPI.Query<RefRO<CreateBonusRequest>>())
            {
                var pos = request.ValueRO.pos;
                if (!gridConfig.IsValidPos(pos))
                    continue;

                var type = request.ValueRO.type;
                if (type == BonusType.None)
                    continue;
                
                int idx = gridConfig.GetIndex(pos);
                if (gridCells[idx].IsEmpty)
                    continue;
                
                var tile = gridCells[idx].tile;
                
                // Don't overwrite existing bonus
                var existingBonus = state.EntityManager.GetComponentData<TileBonusData>(tile);
                if (existingBonus.type != BonusType.None)
                    continue;
                
                state.EntityManager.SetComponentData<TileBonusData>(tile, new() { type = request.ValueRO.type});
            }
            
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            ecb.DestroyEntity(requestQuery, EntityQueryCaptureMode.AtPlayback);
        }
    }
}
