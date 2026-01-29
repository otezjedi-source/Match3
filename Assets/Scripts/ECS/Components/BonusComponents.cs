using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Components
{
    /// <summary>
    /// Assigned bonus type.
    /// </summary>
    public enum BonusType : byte
    {
        LineHorizontal = 1, // Clears entire bonus row.
        LineVertical = 2,   // Clears entire bonus column.
        Bomb = 3,           // Clears 3x3 area around bonus.
    }

    /// <summary>
    /// Marks a tile as having a bonus effect.
    /// Enableable: disabled = normal tile, enabled = bonus tile.
    /// When tile with this component gets MatchTag, BonusActivationSystem triggers its effect.
    /// </summary>
    public struct Bonus : IComponentData, IEnableableComponent
    {
        public BonusType type;
    }

    /// <summary>
    /// Request to assign bonus to a tile at specified position.
    /// Created by BonusDetectionSystem, processed by BonusSpawnSystem.
    /// </summary>
    public struct CreateBonusRequest : IComponentData
    {
        public int2 pos;
        public BonusType type;
    }
}
