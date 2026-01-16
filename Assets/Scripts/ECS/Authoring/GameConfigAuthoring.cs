using Match3.Core;
using UnityEngine;

namespace Match3.ECS.Authoring
{
    /// <summary>
    /// Authoring component for baking GameConfig into ECS components.
    /// Attach to a GameObject in a subscene to make config available to ECS systems.
    /// </summary>
    public class GameConfigAuthoring : MonoBehaviour
    {
        public GameConfig gameConfig;
    }
}
