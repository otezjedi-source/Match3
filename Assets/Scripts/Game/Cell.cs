using UnityEngine;

namespace MiniIT.GAME
{
    public class Cell : MonoBehaviour
    {
        public Vector2Int Position { get; private set; }
        public Tile Tile { get; set; }

        public void Init(Vector2Int position)
        {
            Position = position;
            gameObject.name = $"Cell_{Position.x}_{Position.y}";
        }

        public bool IsEmpty => Tile == null;
    }
}
