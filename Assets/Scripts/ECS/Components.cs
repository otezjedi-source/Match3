using Match3.Game;
using Unity.Entities;

namespace Match3.ECS.Components
{
    public struct GridConfig : IComponentData
    {
        public int Width;
        public int Height;
    }

    [InternalBufferCapacity(64)]
    public struct GridCell : IBufferElementData
    {
        public Entity Tile;
    }

    public struct TileData : IComponentData
    {
        public TileType Type;
    }

    public sealed class TileViewData : IComponentData
    {
        public Tile View;
    }
}
