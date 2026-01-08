using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Components
{
    public struct GridTag : IComponentData { }

    [InternalBufferCapacity(64)]
    public struct GridCell : IBufferElementData
    {
        public Entity Tile;
        public readonly bool IsEmpty => Tile == Entity.Null;
    }

    [InternalBufferCapacity(64)]
    public struct GridTileTypeCache : IBufferElementData
    {
        public TileType Type;
    }

    [InternalBufferCapacity(32)]
    public struct MatchResult : IBufferElementData
    {
        public int2 Pos;
    }

    public struct GridDirtyFlag : IComponentData
    {
        public bool IsDirty;
    }

    public struct PossibleMovesCache : IComponentData
    {
        public bool IsValid;
        public bool HasMoves;
    }

    public struct GridStartRequest : IComponentData { }
    public struct GridResetRequest : IComponentData { }
}
