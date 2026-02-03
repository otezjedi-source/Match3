using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Components
{
    /// <summary>
    /// Tag for querying the config entity.
    /// </summary>
    public struct ConfigTag : IComponentData { }

    /// <summary>
    /// Grid dimensions and utility methods for index conversion.
    /// Baked from GameConfig at startup.
    /// </summary>
    public struct GridConfig : IComponentData
    {
        public int width;
        public int height;
        public int maxInitAttempts;

        public readonly int CellCount => width * height;

        // Convert 2D position to 1D buffer index
        public readonly int GetIndex(int x, int y) => y * width + x;
        public readonly int GetIndex(int2 pos) => GetIndex(pos.x, pos.y);

        // Convert 1D buffer index to 2D position
        public readonly int2 GetPos(int index) => new(index % width, index / width);

        public readonly bool IsValidPos(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
        public readonly bool IsValidPos(int2 pos) => IsValidPos(pos.x, pos.y);
    }

    /// <summary>
    /// Matching rules and scoring. Baked from GameConfig.
    /// </summary>
    public struct MatchConfig : IComponentData
    {
        public int matchCount;      // Tiles needed for a match
        public int pointsPerTile;   // Score per matched tile
    }

    /// <summary>
    /// Bonus element data. Baked from GameConfig.
    /// </summary>
    public struct BonusConfig : IBufferElementData
    {
        public BonusType type; // Created bonus type.
        public int matchCount; // Matched tiles count needed to create the bonus.
    }

    /// <summary>
    /// Animation durations. Baked from GameConfig.
    /// </summary>
    public struct TimingConfig : IComponentData
    {
        public float swapDuration;
        public float fallDuration;
        public float matchDelay;
    }
}
