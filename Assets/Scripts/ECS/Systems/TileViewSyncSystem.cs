using Match3.ECS.Components;
using Unity.Entities;

namespace Match3.ECS.Systems
{
    /// <summary>
    /// Syncs ECS tile positions to MonoBehaviour transforms.
    /// Handles drop squash animation.
    /// </summary>
    [UpdateInGroup(typeof(GameSyncSystemGroup))]
    public partial struct TileViewSyncSystem : ISystem
    {
        private EntityQuery movingQuery;
        private EntityQuery dropQuery;
        private EntityQuery dropDoneQuery;
        private ComponentTypeSet dropDoneRemoveTags;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ManagedReferences>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            movingQuery = SystemAPI.QueryBuilder()
                .WithAll<TileMove, TileWorldPos, TileViewData>()
                .Build();

            dropQuery = SystemAPI.QueryBuilder()
                .WithAll<TileWorldPos, TileStateData, TileViewData>()
                .WithNone<DropTag, DropDoneEvent>()
                .WithDisabled<TileMove>()
                .Build();

            dropDoneQuery = SystemAPI.QueryBuilder()
                .WithAll<DropDoneEvent, DropTag, TileStateData>()
                .Build();

            dropDoneRemoveTags = new(
                ComponentType.ReadWrite<DropTag>(),
                ComponentType.ReadWrite<DropDoneEvent>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            var refs = SystemAPI.ManagedAPI.GetSingleton<ManagedReferences>();

            SyncMovingTiles(ref state);
            SyncBonuses(ref state, refs);
            
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            StartDropAnims(ref state, refs, ecb);
            CompleteDropAnims(ref state, ecb);
        }

        /// <summary>
        /// Update view positions for moving tiles.
        /// </summary>
        private readonly void SyncMovingTiles(ref SystemState state)
        {
            foreach (var (worldPos, viewData) in 
                     SystemAPI.Query<RefRO<TileWorldPos>, TileViewData>()
                         .WithAll<TileMove>())
            {
                if (viewData.view is not null)
                    viewData.view.transform.position = worldPos.ValueRO.pos;
            }
        }

        private readonly void SyncBonuses(ref SystemState state, ManagedReferences refs)
        {
            if (refs.dataCache == null)
                return;

            foreach (var (bonusData, viewData) in
                     SystemAPI.Query<RefRO<TileBonusData>, TileViewData>()
                         .WithChangeFilter<TileBonusData>())
            {
                if (viewData.view is null)
                    continue;

                refs.dataCache.TryGet(bonusData.ValueRO.type, out var data);
                viewData.view.SetBonus(data).Forget();
            }
        }

        /// <summary>
        /// Start squash animation when tile lands.
        /// </summary>
        private readonly void StartDropAnims(ref SystemState state, ManagedReferences refs, EntityCommandBuffer ecb)
        {
            bool playSound = false;

            foreach (var (worldPos, stateData, viewData, entity) in 
                     SystemAPI.Query<RefRO<TileWorldPos>, RefRO<TileStateData>, TileViewData>()
                         .WithDisabled<TileMove>() 
                         .WithNone<DropTag, DropDoneEvent>() 
                         .WithEntityAccess())
            {
                if (viewData.view is null)
                    continue;

                viewData.view.transform.position = worldPos.ValueRO.pos;

                if (stateData.ValueRO.state != TileState.Fall)
                    continue;
                
                viewData.view.PlayDropAnim();
                ecb.AddComponent<DropTag>(entity);
                playSound = true;
            }

            if (playSound)
                refs.soundController?.Play(SoundType.Drop);
        }

        private readonly void CompleteDropAnims(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach (var stateData in
                SystemAPI.Query<RefRW<TileStateData>>()
                    .WithAll<DropDoneEvent,DropTag>())
            {
                stateData.ValueRW.state = TileState.Idle;
            }
            
            ecb.RemoveComponent(dropDoneQuery, dropDoneRemoveTags, EntityQueryCaptureMode.AtPlayback);
        }
    }
}
