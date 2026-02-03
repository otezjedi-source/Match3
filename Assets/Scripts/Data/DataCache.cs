using System.Collections.Generic;
using Match3.Core;
using Match3.ECS.Components;
using UnityEngine;
using VContainer;

namespace Match3.Data
{
    public class DataCache
    {
        private Dictionary<TileType, GameConfig.TileData> tilesCache;
        private Dictionary<BonusType, GameConfig.BonusData> bonusesCache;
        
        [Inject]
        public DataCache(GameConfig gameConfig)
        {
            CreateTilesCache(gameConfig.tilesData);
            CreateBonusesCache(gameConfig.bonusesData);
        }

        private void CreateTilesCache(List<GameConfig.TileData> tilesData)
        {
            tilesCache = new(tilesData.Count);
            foreach (var data in tilesData)
            {
                if (!tilesCache.TryAdd(data.type, data))
                    Debug.LogWarning($"[DataCache] Duplicate TileType in game config: {data.type}");
            }
        }
        
        private void CreateBonusesCache(List<GameConfig.BonusData> bonusesData)
        {
            bonusesCache = new(bonusesData.Count);
            foreach (var data in bonusesData)
            {
                if (!bonusesCache.TryAdd(data.type, data))
                    Debug.LogWarning($"[DataCache] Duplicate BonusType in game config: {data.type}");
            }
        }
        
        public bool TryGet(TileType type, out GameConfig.TileData data) => tilesCache.TryGetValue(type, out data);
        public bool TryGet(BonusType type, out GameConfig.BonusData data) => bonusesCache.TryGetValue(type, out data);
    }
}