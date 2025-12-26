using System.Collections.Generic;
using Match3.Core;
using Match3.ECS.Components;
using Match3.Game;
using Unity.Entities;
using UnityEngine;
using VContainer;

namespace Match3.Factories
{
    public class TileFactory
    {
        [Inject] private readonly Tile tilePrefab;
        [Inject] private readonly Transform parent;
        [Inject] private readonly GameConfig config;
        [Inject] private readonly EntityManager entityManager;

        private readonly Queue<Entity> pool = new();
        private readonly Dictionary<Entity, Tile> views = new();

        public Entity Create(int x, int y, TileType type)
        {
            Entity entity;
            Tile view;

            if (pool.Count > 0)
            {
                entity = pool.Dequeue();
                view = views[entity];
                view.gameObject.SetActive(true);
                view.transform.position = new(x, y);
                entityManager.SetComponentData(entity, new TileData { Type = type });
            }
            else
            {
                entity = entityManager.CreateEntity();
                view = Object.Instantiate(tilePrefab, new(x, y), Quaternion.identity, parent);
                entityManager.AddComponentData(entity, new TileData { Type = type });
                entityManager.AddComponentData(entity, new TileViewData { View = view });
                views[entity] = view;
            }

            var data = config.TilesData.Find(s => s.type == type);
            view.Init(data);

            return entity;
        }

        public void Return(Entity entity)
        {
            if (!views.TryGetValue(entity, out var view))
                return;

            view.gameObject.SetActive(false);
            pool.Enqueue(entity);
        }

        public Tile GetView(Entity entity)
        {
            return views.TryGetValue(entity, out var view) ? view : null;
        }

        public Entity GetEntity(Tile view)
        {
            foreach (var pair in views)
            {
                if (pair.Value == view)
                    return pair.Key;
            }

            return Entity.Null;
        }
    }
}
