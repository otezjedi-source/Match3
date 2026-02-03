using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Core;
using Match3.Data;
using Match3.ECS.Components;
using Match3.Game;
using Unity.Entities;
using UnityEngine;
using VContainer;

namespace Match3.Factories
{
    /// <summary>
    /// Factory for creating and pooling tile entities.
    /// Each tile consists of an ECS entity (data) and a TileView (visual).
    /// Pooling prevents GC allocations during gameplay.
    /// </summary>
    public class TileFactory : IDisposable
    {
        [Inject] private readonly TileView tilePrefab;
        [Inject] private readonly Transform parent;
        [Inject] private readonly GameConfig gameConfig;
        [Inject] private readonly DataCache dataCache;
        [Inject] private readonly EntityManager entityManager;

        private TilePool pool;
        private TileViewInitializer initializer;

        private bool isDisposed;

        public void Init()
        {
            if (isDisposed)
                throw new ObjectDisposedException("[TileFactory] Trying to init disposed");
            
            pool = new(tilePrefab, parent, entityManager);
            initializer = new(dataCache);
        }

        /// <summary>
        /// Get a tile from pool or create new one.
        /// Position is in grid coordinates (will be converted to world space).
        /// </summary>
        public Entity Create(int x, int y, TileType type)
        {
            if (isDisposed)
                throw new ObjectDisposedException("[TileFactory] Trying to create in disposed");
            
            if (!pool.TryGet(out var entity, out var view))
                (entity, view) = pool.Create(x, y);
            else
            {
                view.gameObject.SetActive(true);
                view.transform.position = new(x, y);
            }

            entityManager.SetComponentData<TileData>(entity, new() { type = type, gridPos = new(x, y) });
            entityManager.SetComponentData<TileBonusData>(entity, new() { type = BonusType.None });
            entityManager.SetComponentData<TileStateData>(entity, new() { state = TileState.Idle });
            entityManager.SetComponentData<TileWorldPos>(entity, new() { pos = new(x, y, 0) });
            entityManager.SetComponentEnabled<TileMove>(entity, false);
            
            initializer.Init(view, type, entityManager, entity);

            return entity;
        }
        
        /// <summary>
        /// Return tile to pool for reuse. Clears visual state but keeps entity alive.
        /// </summary>
        public void Return(Entity entity)
        {
            if (!isDisposed)
                pool.Return(entity);
        }

        /// <summary>
        /// Waits for all tiles loading operations to complete
        /// </summary>
        public UniTask WaitForLoading(CancellationToken ct = default) => initializer.WaitAllAsync(ct);

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            initializer.Dispose();
            pool.Clear();
        }
    }
}
