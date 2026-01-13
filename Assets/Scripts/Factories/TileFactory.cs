using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.ECS.Components;
using Match3.Game;
using Unity.Entities;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace Match3.Factories
{
    public class TileFactory : IDisposable
    {
        [Inject] private readonly TileView tilePrefab;
        [Inject] private readonly Transform parent;
        [Inject] private readonly GameConfig gameConfig;
        [Inject] private readonly EntityManager entityManager;

        private readonly Queue<Entity> pool = new();
        private Dictionary<TileType, GameConfig.TileData> tileDataCache;
        private bool isDisposed;

        public void Init()
        {
            if (isDisposed)
                throw new ObjectDisposedException("[TileFactory] Trying to init disposed");

            tileDataCache = new(gameConfig.TilesData.Count);
            foreach (var tileData in gameConfig.TilesData)
            {
                if (!tileDataCache.ContainsKey(tileData.type))
                    tileDataCache[tileData.type] = tileData;
                else
                    Debug.LogWarning($"[TileFactory] Duplicate TileType in game config: {tileData.type}");
            }
        }

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

                if (!entityManager.Exists(entity))
                {
                    Debug.LogWarning("[TileFactory] Tile entity doesn't exist. Creating new tile");
                    return CreateTile(x, y, type);
                }

                entityManager.SetComponentData<TileData>(entity, new() { Type = type, GridPos = new(x, y) });
                entityManager.SetComponentData<TileStateData>(entity, new() { State = TileState.Idle });
                entityManager.SetComponentData<TileWorldPos>(entity, new() { Pos = new(x, y, 0) });
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

            InitView(view, type, entity);
            return entity;
        }

        private Entity CreateTile(int x, int y, TileType type)
        {
            var entity = entityManager.CreateEntity();
            var view = Object.Instantiate(tilePrefab, new(x, y), Quaternion.identity, parent);

            entityManager.AddComponentData<TileData>(entity, new() { Type = type, GridPos = new(x, y) });
            entityManager.AddComponentData<TileStateData>(entity, new() { State = TileState.Idle });
            entityManager.AddComponentData<TileWorldPos>(entity, new() { Pos = new(x, y, 0) });
            entityManager.AddComponentData<TileMove>(entity, new());
            entityManager.AddComponentObject(entity, new TileViewData { View = view });
            entityManager.SetComponentEnabled<TileMove>(entity, false);

            InitView(view, type, entity);
            return entity;
        }

        private void InitView(TileView view, TileType type, Entity entity)
        {
            if (!tileDataCache.TryGetValue(type, out var data))
            {
                Debug.LogWarning($"[TileFactory] Data not found for type: {type}");
                return;
            }
            view.Init(data, entityManager, entity);
        }

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
                entityManager.DestroyEntity(entity);
            }
        }

        public TileView GetView(Entity entity)
        {
            if (!entityManager.Exists(entity) || !entityManager.HasComponent<TileViewData>(entity))
                return null;
            return entityManager.GetComponentObject<TileViewData>(entity)?.View;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            if (World.DefaultGameObjectInjectionWorld?.IsCreated == true)
            {
                pool.Clear();
                tileDataCache?.Clear();
                return;
            }

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
