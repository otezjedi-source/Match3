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
        [Inject] private readonly TileView tilePrefab;
        [Inject] private readonly Transform parent;
        [Inject] private readonly GameConfig config;
        [Inject] private readonly EntityManager entityMgr;

        private readonly Queue<Entity> pool = new();

        public Entity Create(int x, int y, TileType type)
        {
            Entity entity;
            TileView view;

            if (pool.Count > 0)
            {
                entity = pool.Dequeue();
                entityMgr.SetComponentData<TileData>(entity, new() { Type = type, GridPos = new(x, y) });
                entityMgr.SetComponentData<TileStateData>(entity, new() { State = TileState.Idle });
                entityMgr.SetComponentData<TileWorldPos>(entity, new() { Pos = new(x, y, 0) });
                entityMgr.SetComponentEnabled<TileMove>(entity, false);

                view = GetView(entity);
                view.gameObject.SetActive(true);
                view.transform.position = new(x, y);
            }
            else
            {
                entity = entityMgr.CreateEntity();
                view = Object.Instantiate(tilePrefab, new(x, y), Quaternion.identity, parent);

                entityMgr.AddComponentData<TileData>(entity, new() { Type = type, GridPos = new(x, y) });
                entityMgr.AddComponentData<TileStateData>(entity, new() { State = TileState.Idle });
                entityMgr.AddComponentData<TileWorldPos>(entity, new() { Pos = new(x, y, 0) });
                entityMgr.AddComponentData<TileMove>(entity, new());
                entityMgr.AddComponentObject(entity, new TileViewData { View = view });
                entityMgr.SetComponentEnabled<TileMove>(entity, false);
            }

            var data = config.TilesData.Find(s => s.type == type);
            view.Init(data);

            return entity;
        }

        public void Return(Entity entity)
        {
            var view = GetView(entity);
            if (view == null)
                return;

            view.Clear();
            view.gameObject.SetActive(false);
            pool.Enqueue(entity);
        }

        public TileView GetView(Entity entity)
        {
            if (!entityMgr.HasComponent<TileViewData>(entity))
                return null;
            return entityMgr.GetComponentObject<TileViewData>(entity)?.View;
        }
    }
}
