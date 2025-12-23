using Match3.Game;
using UnityEngine;
using VContainer;

namespace Match3.Factories
{
    public class CellFactory
    {
        [Inject] private readonly Cell cellPrefab;
        [Inject] private readonly Transform parent;

        public Cell Create(Vector2Int position, Vector3 worldPosition)
        {
            var cell = Object.Instantiate(cellPrefab, worldPosition, Quaternion.identity, parent);
            cell.Init(position);
            return cell;
        }
    }
}
