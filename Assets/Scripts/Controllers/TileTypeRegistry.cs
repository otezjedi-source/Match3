using System;
using Match3.Core;
using Match3.ECS.Components;
using VContainer;
using Random = UnityEngine.Random;

namespace Match3.Controllers
{
    public class TileTypeRegistry
    {
        private readonly TileType[] types;

        public ReadOnlySpan<TileType> All => types;

        [Inject]
        public TileTypeRegistry(GameConfig gameConfig)
        {
            types = new TileType[gameConfig.TilesData.Count];
            for (int i = 0; i < types.Length; i++)
                types[i] = gameConfig.TilesData[i].type;
        }

        public TileType GetRandomType()
        {
            int idx = Random.Range(0, types.Length);
            return types[idx];
        }
    }
}
