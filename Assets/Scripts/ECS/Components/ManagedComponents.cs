using Match3.Controllers;
using Match3.Core;
using Match3.Data;
using Match3.Factories;
using Match3.Game;
using Unity.Entities;

namespace Match3.ECS.Components
{
    /// <summary>
    /// Managed component linking tile entity to its visual representation.
    /// Cannot use IComponentData (unmanaged) because TileView is a class.
    /// </summary>
    public class TileViewData : IComponentData
    {
        public TileView view;
    }

    /// <summary>
    /// Singleton with references to managed objects (controllers, factories).
    /// Allows ECS systems to call managed code (audio, spawning, scoring).
    /// Created by GameInitializer on scene load.
    /// </summary>
    public class ManagedReferences : IComponentData
    {
        public ScoreController scoreController;
        public SoundController soundController;
        public TileTypeRegistry tileTypeRegistry;
        public DataCache dataCache;
        public TileFactory tileFactory;
    }
}
