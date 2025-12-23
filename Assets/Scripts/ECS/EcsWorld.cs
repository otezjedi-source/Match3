using System;
using System.Collections.Generic;

namespace MiniIT.ECS
{
    public class EcsWorld
    {
        private int _nextEntityId = 0;
        private readonly Dictionary<Type, object> _componentPools = new Dictionary<Type, object>();
        private readonly HashSet<int> _entities = new HashSet<int>();

        public Entity CreateEntity()
        {
            var entity = new Entity(_nextEntityId++);
            _entities.Add(entity.Id);
            return entity;
        }

        public void DestroyEntity(Entity entity)
        {
            if (!_entities.Contains(entity.Id))
                return;

            foreach (var pool in _componentPools.Values)
            {
                var dictType = pool.GetType();
                var removeMethod = dictType.GetMethod("Remove");
                removeMethod?.Invoke(pool, new object[] { entity.Id });
            }

            _entities.Remove(entity.Id);
        }

        public T AddComponent<T>(Entity entity) where T : class, IEcsComponent, new()
        {
            var pool = GetOrCreatePool<T>();
            var component = new T();
            pool[entity.Id] = component;
            return component;
        }

        public T GetComponent<T>(Entity entity) where T : class, IEcsComponent
        {
            var pool = GetOrCreatePool<T>();
            pool.TryGetValue(entity.Id, out var component);
            return component;
        }

        public bool HasComponent<T>(Entity entity) where T : class, IEcsComponent
        {
            var pool = GetOrCreatePool<T>();
            return pool.ContainsKey(entity.Id);
        }

        public void RemoveComponent<T>(Entity entity) where T : class, IEcsComponent
        {
            var pool = GetOrCreatePool<T>();
            pool.Remove(entity.Id);
        }

        public IEnumerable<Entity> GetEntitiesWithComponent<T>() where T : class, IEcsComponent
        {
            var pool = GetOrCreatePool<T>();
            foreach (var entityId in pool.Keys)
            {
                yield return new Entity(entityId);
            }
        }

        public IEnumerable<(Entity entity, T component)> GetEntitiesAndComponents<T>() where T : class, IEcsComponent
        {
            var pool = GetOrCreatePool<T>();
            foreach (var kvp in pool)
            {
                yield return (new Entity(kvp.Key), kvp.Value);
            }
        }

        public void Clear()
        {
            _entities.Clear();
            _componentPools.Clear();
            _nextEntityId = 0;
        }

        private Dictionary<int, T> GetOrCreatePool<T>() where T : class, IEcsComponent
        {
            var type = typeof(T);
            if (!_componentPools.TryGetValue(type, out var pool))
            {
                pool = new Dictionary<int, T>();
                _componentPools[type] = pool;
            }
            return (Dictionary<int, T>)pool;
        }
    }
}
