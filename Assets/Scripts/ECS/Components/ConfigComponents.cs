using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Components
{
    /// <summary>
    /// Tag for querying the config entity.
    /// </summary>
    public struct GameConfigTag : IComponentData { }

    /// <summary>
    /// Grid dimensions and utility methods for index conversion.
    /// Baked from GameConfig at startup.
    /// </summary>
    public struct GridConfig : IComponentData
    {
        public int Width;
        public int Height;
        public int MaxInitAttempts;

        public readonly int CellCount => Width * Height;

        // Convert 2D position to 1D buffer index
        public readonly int GetIndex(int x, int y) => y * Width + x;
        public readonly int GetIndex(int2 pos) => GetIndex(pos.x, pos.y);

        // Convert 1D buffer index to 2D position
        public readonly int2 GetPos(int index) => new(index % Width, index / Width);

        public readonly bool IsValidPos(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public readonly bool IsValidPos(int2 pos) => IsValidPos(pos.x, pos.y);
    }

    /// <summary>
    /// Matching rules and scoring. Baked from GameConfig.
    /// </summary>
    public struct MatchConfig : IComponentData
    {
        public int MatchCount;      // Tiles needed for a match
        public int PointsPerTile;   // Score per matched tile
    }

    /// <summary>
    /// Animation durations. Baked from GameConfig.
    /// </summary>
    public struct TimingConfig : IComponentData
    {
        public float SwapDuration;
        public float FallDuration;
        public float MatchDelay;
    }
}
