using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Core;
using Match3.ECS.Components;
using Match3.Game;
using Unity.Entities;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

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
        [Inject] private readonly EntityManager entityManager;

        private readonly Queue<Entity> pool = new();
        private readonly HashSet<UniTask> loadingTasks = new();

        // O(1) lookup instead of List.Find() on every tile creation
        private Dictionary<TileType, GameConfig.TileData> tileDataCache;
        private CancellationTokenSource cts;
        private bool isDisposed;

        public void Init()
        {
            if (isDisposed)
                throw new ObjectDisposedException("[TileFactory] Trying to init disposed");

            cts = new();

            tileDataCache = new(gameConfig.tilesData.Count);
            foreach (var tileData in gameConfig.tilesData)
            {
                if (!tileDataCache.ContainsKey(tileData.type))
                    tileDataCache[tileData.type] = tileData;
                else
                    Debug.LogWarning($"[TileFactory] Duplicate TileType in game config: {tileData.type}");
            }
        }

        /// <summary>
        /// Get a tile from pool or create new one.
        /// Position is in grid coordinates (will be converted to world space).
        /// </summary>
        public Entity Create(int x, int y, TileType type)
        {
            if (isDisposed)
                throw new ObjectDisposedException("[TileFactory] Trying to create in disposed");

            if (tileDataCache == null)
                Init();

            Entity entity;
            TileView view;

            if (pool.Count > 0)
            {
                entity = pool.Dequeue();

                // Entity might have been destroyed externally
                if (!entityManager.Exists(entity))
                {
                    Debug.LogWarning("[TileFactory] Tile entity doesn't exist. Creating new tile");
                    return CreateTile(x, y, type);
                }

                // Reset entity components to new state
                entityManager.SetComponentData<TileData>(entity, new() { type = type, gridPos = new(x, y) });
                entityManager.SetComponentData<TileStateData>(entity, new() { state = TileState.Idle });
                entityManager.SetComponentData<TileWorldPos>(entity, new() { pos = new(x, y, 0) });
                entityManager.SetComponentEnabled<TileMove>(entity, false);

                view = GetView(entity);
                if (view == null || view.gameObject == null)
                {
                    Debug.LogWarning("[TileFactory] Pooled view was destroyed. Creating new tile");
                    entityManager.DestroyEntity(entity);
                    return CreateTile(x, y, type);
                }

                view.gameObject.SetActive(true);
                view.transform.position = new(x, y);
            }
            else
                return CreateTile(x, y, type);

            var task = InitViewAsync(view, type, entity);
            loadingTasks.Add(task);
            return entity;
        }

        private Entity CreateTile(int x, int y, TileType type)
        {
            var entity = entityManager.CreateEntity();
            var view = Object.Instantiate(tilePrefab, new(x, y), Quaternion.identity, parent);

            // Add all required components
            entityManager.AddComponentData<TileData>(entity, new() { type = type, gridPos = new(x, y) });
            entityManager.AddComponentData<TileStateData>(entity, new() { state = TileState.Idle });
            entityManager.AddComponentData<TileWorldPos>(entity, new() { pos = new(x, y, 0) });
            entityManager.AddComponentData<TileMove>(entity, new());
            entityManager.AddComponentObject(entity, new TileViewData { view = view });
            entityManager.SetComponentEnabled<TileMove>(entity, false);

            var task = InitViewAsync(view, type, entity);
            loadingTasks.Add(task);
            return entity;
        }

        private async UniTask InitViewAsync(TileView view, TileType type, Entity entity)
        {
            if (!tileDataCache.TryGetValue(type, out var data))
            {
                Debug.LogWarning($"[TileFactory] Data not found for type: {type}");
                return;
            }

            try
            {
                if (cts == null || cts.IsCancellationRequested)
                    return;

                await view.InitAsync(data, entityManager, entity);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[TileFactory] Failed to init view: {ex.Message}");
            }
        }

        /// <summary>
        /// Waits for all tiles loading operations to complete
        /// </summary>
        public async UniTask WaitForTilesLoaded(CancellationToken ct = default)
        {
            if (isDisposed || loadingTasks.Count == 0)
                return;

            var tasks = new UniTask[loadingTasks.Count];
            loadingTasks.CopyTo(tasks);
            loadingTasks.Clear();

            using var linkedCts = cts != null
                ? CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct)
                : CancellationTokenSource.CreateLinkedTokenSource(ct);

            try
            {
                await UniTask.WhenAll(tasks).AttachExternalCancellation(linkedCts.Token);
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Return tile to pool for reuse. Clears visual state but keeps entity alive.
        /// </summary>
        public void Return(Entity entity)
        {
            if (isDisposed || !entityManager.Exists(entity))
                return;

            var view = GetView(entity);
            if (view == null || view.gameObject == null)
            {
                entityManager.DestroyEntity(entity);
                return;
            }

            try
            {
                view.Clear();
                view.gameObject.SetActive(false);
                pool.Enqueue(entity);
            }
            catch (MissingReferenceException)
            {
                // View was destroyed during Clear(), just cleanup entity
                entityManager.DestroyEntity(entity);
            }
        }

        public TileView GetView(Entity entity)
        {
            if (!entityManager.Exists(entity) || !entityManager.HasComponent<TileViewData>(entity))
                return null;
            return entityManager.GetComponentObject<TileViewData>(entity)?.view;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            if (cts != null)
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch (ObjectDisposedException) { }
                cts = null;
            }

            loadingTasks.Clear();

            if (World.DefaultGameObjectInjectionWorld?.IsCreated == true)
            {
                pool.Clear();
                tileDataCache?.Clear();
                return;
            }

            // Cleanup all pooled entities and their views
            while (pool.Count > 0)
            {
                var entity = pool.Dequeue();
                if (entityManager.Exists(entity))
                {
                    var view = GetView(entity);
                    if (view != null && view.gameObject != null)
                        Object.Destroy(view.gameObject);
                    entityManager.DestroyEntity(entity);
                }
            }

            var query = entityManager.CreateEntityQuery(typeof(TileData));
            entityManager.DestroyEntity(query);
            query.Dispose();

            tileDataCache?.Clear();
            tileDataCache = null;
        }
    }
}
