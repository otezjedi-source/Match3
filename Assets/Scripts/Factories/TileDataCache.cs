using System.Collections.Generic;
using Match3.Core;
using Match3.ECS.Components;
using UnityEngine;

namespace Match3.Factories
{
    public class TileDataCache
    {
        private readonly Dictionary<TileType, GameConfig.TileData> cache;
        
        public TileDataCache(GameConfig gameConfig)
        {
            cache = new(gameConfig.tilesData.Count);
            foreach (var tileData in gameConfig.tilesData)
            {
                if (!cache.TryAdd(tileData.type, tileData))
                    Debug.LogWarning($"[TileDataCache] Duplicate TileType in game config: {tileData.type}");
            }
        }
        
        public bool TryGet(TileType type, out GameConfig.TileData data) => cache.TryGetValue(type, out data);
    }
}