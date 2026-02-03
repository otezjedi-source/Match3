using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Components
{
    /// <summary>
    /// Tile color/type. None indicates empty cell.
    /// </summary>
    public enum TileType : byte
    {
        None = 0,
        Red = 1,
        Green = 2,
        Blue = 3,
        Yellow = 4,
        Purple = 5,
    }

    // <summary>
    /// Current animation state of a tile.
    /// </summary>
    public enum TileState : byte
    {
        Idle = 0,
        Swap = 1,
        Clear = 2,
        Fall = 3,
    }
    
    /// <summary>
    /// Assigned bonus type. None indicates no bonus
    /// </summary>
    public enum BonusType : byte
    {
        None = 0,
        LineHorizontal = 1, // Clears entire bonus row
        LineVertical = 2,   // Clears entire bonus column
        Bomb = 3,           // Clears 3x3 area around bonus
    }

    /// <summary>
    /// Core tile data: type and grid position.
    /// </summary>
    public struct TileData : IComponentData
    {
        public TileType type;
        public int2 gridPos;
    }
    
    /// <summary>
    /// Marks a tile as having a bonus effect.
    /// When tile with this component gets MatchTag, BonusActivationSystem triggers its effect.
    /// </summary>
    public struct TileBonusData : IComponentData
    {
        public BonusType type;
    }

    public struct TileStateData : IComponentData
    {
        public TileState state;
    }

    /// <summary>
    /// World position for rendering. Updated by TileMoveSystem during animations.
    /// </summary>
    public struct TileWorldPos : IComponentData
    {
        public float3 pos;
    }

    /// <summary>
    /// Enableable component for tile movement animation.
    /// When enabled, TileMoveSystem interpolates position from StartPos to TargetPos.
    /// </summary>
    public struct TileMove : IComponentData, IEnableableComponent
    {
        public float3 startPos;
        public float3 targetPos;
        public float duration;
        public float elapsed;
    }
    
    /// <summary>
    /// Request to assign bonus to a tile at specified position
    /// Created by BonusDetectSystem, processed by BonusInitSystem
    /// </summary>
    public struct CreateBonusRequest : IComponentData
    {
        public int2 pos;
        public BonusType type;
    }

    // Tags for tile state tracking

    /// <summary>
    /// Tile is part of a match and will be cleared.
    /// </summary>
    public struct MatchTag : IComponentData { }

    /// <summary>
    /// Tile clear animation is in progress.
    /// </summary>
    public struct ClearTag : IComponentData { }

    /// <summary>
    /// Clear animation finished (set by TileView).
    /// </summary>
    public struct ClearDoneEvent : IComponentData { }

    /// <summary>
    /// Tile drop animation is in progress.
    /// </summary>
    public struct DropTag : IComponentData { }

    /// <summary>
    /// Drop animation finished (set by TileView).
    /// </summary>
    public struct DropDoneEvent : IComponentData { }
}
