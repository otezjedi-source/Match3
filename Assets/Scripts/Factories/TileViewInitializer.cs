using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.ECS.Components;
using Match3.Game;
using Unity.Entities;
using UnityEngine;

namespace Match3.Factories
{
    public class TileViewInitializer : IDisposable
    {
        private readonly TileDataCache dataCache;
        private readonly HashSet<UniTask> tasks = new();

        private CancellationTokenSource cts;
        private bool isDisposed;

        public TileViewInitializer(TileDataCache dataCache)
        {
            this.dataCache = dataCache;
            cts = new();
        }

        public void Init(TileView view, TileType type, EntityManager entityManager, Entity entity)
        {
            if (isDisposed)
                return;

            var task = InitViewAsync(view, type, entityManager, entity);
            tasks.Add(task);
        }

        private async UniTask InitViewAsync(TileView view, TileType type, EntityManager entityManager, Entity entity)
        {
            if (!dataCache.TryGet(type, out var data))
            {
                Debug.LogWarning($"[TileViewInitializer] Data not found for type: {type}");
                return;
            }

            try
            {
                await view.InitAsync(data, entityManager, entity);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[TileViewInitializer] Init failed: {ex.Message}");
            }
        }

        public async UniTask WaitAllAsync(CancellationToken ct = default)
        {
            if (isDisposed || tasks.Count == 0)
                return;
            
            var currentTasks = new UniTask[tasks.Count];
            tasks.CopyTo(currentTasks);
            tasks.Clear();
            
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
            try
            {
                await UniTask.WhenAll(tasks).AttachExternalCancellation(linked.Token);
            }
            catch (OperationCanceledException) { }
        }
        
        public void Dispose()
        {
            if (isDisposed)
                return;
            
            isDisposed = true;
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            tasks.Clear();
        }
    }
}