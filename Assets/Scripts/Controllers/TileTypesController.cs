using System.Collections.Generic;
using Match3.Core;
using Match3.Game;
using UnityEngine;
using VContainer;

namespace Match3.Controllers
{
    public class TileTypesController
    {
        [Inject] private readonly GameConfig config;

        List<TileType> types;

        private void Init()
        {
            types = new(config.TilesData.Count);
            foreach (var data in config.TilesData)
                types.Add(data.type);
        }

        public IReadOnlyList<TileType> GetAllTypes()
        {
            if (types == null)
                Init();
            return types;
        }

        public TileType GetRandomType()
        {
            if (types == null)
                Init();
            int idx = Random.Range(0, types.Count);
            return types[idx];
        }
    }
}
