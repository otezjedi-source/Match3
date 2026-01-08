using Match3.Controllers;
using Match3.Factories;
using Match3.Game;
using Unity.Entities;

namespace Match3.ECS.Components
{
    public class TileViewData : IComponentData
    {
        public TileView View;
    }

    public class ManagedReferences : IComponentData
    {
        public ScoreController ScoreController;
        public SoundController SoundController;
        public TileTypeRegistry TileTypeRegistry;
        public TileFactory TileFactory;
    }
}
