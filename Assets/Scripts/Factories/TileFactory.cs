using System.Collections.Generic;
using MiniIT.CORE;
using MiniIT.GAME;
using UnityEngine;
using VContainer;

namespace MiniIT.FACTORIES
{
    public class TileFactory
    {
        [Inject] private readonly Tile tilePrefab;
        [Inject] private readonly Transform parent;
        [Inject] private readonly GameConfig config;

        private readonly Queue<Tile> pool = new Queue<Tile>();

        public Tile Create(Vector3 position)
        {
            Tile tile;
        
            if (pool.Count > 0)
            {
                tile = pool.Dequeue();
                tile.gameObject.SetActive(true);
                tile.transform.position = position;
            }
            else
            {
                tile = Object.Instantiate(tilePrefab, position, Quaternion.identity, parent);
            }
            
            var rnd = Random.Range(0, config.TilesData.Count);
            tile.Init(config.TilesData[rnd]);
            tile.gameObject.name = $"Tile_{tile.Type}_{position.x}_{position.y}";
            return tile;
        }

        public Tile CreateSpecificType(Vector3 position, TileType type)
        {
            Tile tile;

            if (pool.Count > 0)
            {
                tile = pool.Dequeue();
                tile.gameObject.SetActive(true);
                tile.transform.position = position;
            }
            else
            {
                tile = Object.Instantiate(tilePrefab, position, Quaternion.identity, parent);
            }

            var data = config.TilesData.Find(s => s.type == type);
            tile.Init(data);
            tile.gameObject.name = $"Tile_{tile.Type}_{position.x}_{position.y}";
            return tile;
        }

        public void Return(Tile tile)
        {
            tile.gameObject.SetActive(false);
            pool.Enqueue(tile);
        }
    }
}
