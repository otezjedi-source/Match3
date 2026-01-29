using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Components
{
    /// <summary>
    /// Tag for the grid singleton entity.
    /// </summary>
    public struct GridTag : IComponentData { }

    /// <summary>
    /// Grid cell containing a tile entity reference.
    /// Stored in a DynamicBuffer on the grid entity.
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct GridCell : IBufferElementData
    {
        public Entity tile;
        public readonly bool IsEmpty => tile == Entity.Null;
    }

    /// <summary>
    /// Cached tile types for fast matching. Updated by GridCacheSystem.
    /// Avoids repeated entity lookups during match detection.
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct GridTileTypeCache : IBufferElementData
    {
        public TileType type;
    }

    /// <summary>
    /// Positions of matched tiles. Populated by MatchSystem, consumed by ClearSystem.
    /// </summary>
    [InternalBufferCapacity(32)]
    public struct MatchResult : IBufferElementData
    {
        public int2 pos;
    }

    /// <summary>
    /// Dirty flag for grid state. When true, GridCacheSystem rebuilds the type cache.
    /// Set by any system that modifies the grid.
    /// </summary>
    public struct GridDirtyFlag : IComponentData
    {
        public bool isDirty;
    }

    /// <summary>
    /// Cache for possible moves check. Invalidated when grid changes.
    /// Prevents redundant expensive move calculations.
    /// </summary>
    public struct PossibleMovesCache : IComponentData
    {
        public bool isValid;    // True if HasMoves is current
        public bool hasMoves;   // True if at least one valid move exists
    }

    // Request singletons - created to trigger systems, destroyed when processed

    /// <summary>
    /// Request to generate initial grid.
    /// </summary>
    public struct GridStartRequest : IComponentData { }

    /// <summary>
    /// Request to reset grid for new game.
    /// </summary>
    public struct GridResetRequest : IComponentData { }
}
