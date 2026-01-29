using System.Collections.Generic;
using Match3.ECS.Components;
using Match3.Game;
using Unity.Entities;
using UnityEngine;

namespace Match3.Factories
{
    public class TilePool
    {
        private readonly TileView prefab;
        private readonly Transform parent;
        private readonly EntityManager entityManager;

        private readonly Queue<Entity> pool = new();

        public TilePool(TileView prefab, Transform parent, EntityManager entityManager)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.entityManager = entityManager;
        }

        public bool TryGet(out Entity entity, out TileView view)
        {
            while (pool.TryDequeue(out entity))
            {
                if (!entityManager.Exists(entity))
                    continue;
                
                view = GetView(entity);
                if (view != null)
                    return true;
                
                entityManager.DestroyEntity(entity);
            }
            
            entity = default;
            view = null;
            return false;
        }

        public (Entity entity, TileView view) Create(int x, int y)
        {
            var entity = entityManager.CreateEntity();
            var view = Object.Instantiate(prefab, new(x, y), Quaternion.identity, parent);
            
            entityManager.AddComponentData<TileData>(entity, default);
            entityManager.AddComponentData<TileStateData>(entity, default);
            entityManager.AddComponentData<TileWorldPos>(entity, default);
            entityManager.AddComponentData<TileMove>(entity, default);
            entityManager.AddComponentObject(entity, new TileViewData { view = view });
            entityManager.SetComponentEnabled<TileMove>(entity, false);

            return (entity, view);
        }

        public void Return(Entity entity)
        {
            if (!entityManager.Exists(entity))
                return;
            
            var view = GetView(entity);
            if (view == null)
            {
                entityManager.DestroyEntity(entity);
                return;
            }

            view.Clear();
            view.gameObject.SetActive(false);
            pool.Enqueue(entity);
        }
        
        public void Clear() => pool.Clear();

        private TileView GetView(Entity entity)
        {
            if (!entityManager.HasComponent<TileViewData>(entity))
                return null;
            return entityManager.GetComponentData<TileViewData>(entity)?.view;
        }
    }
}