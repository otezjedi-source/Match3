using System;
using Match3.Core;
using Match3.ECS.Components;
using VContainer;
using Random = UnityEngine.Random;

namespace Match3.Data
{
    /// <summary>
    /// Registry of available tile types. Used by FillSystem to spawn random tiles.
    /// Initialized from GameConfig at startup.
    /// </summary>
    public class TileTypeRegistry
    {
        private readonly TileType[] types;

        public ReadOnlySpan<TileType> All => types;

        [Inject]
        public TileTypeRegistry(GameConfig gameConfig)
        {
            types = new TileType[gameConfig.tilesData.Count];
            for (int i = 0; i < types.Length; i++)
                types[i] = gameConfig.tilesData[i].type;
        }

        /// <summary>
        /// Get a random tile type for spawning new tiles.
        /// </summary>
        public TileType GetRandomType()
        {
            int idx = Random.Range(0, types.Length);
            return types[idx];
        }
    }
}
