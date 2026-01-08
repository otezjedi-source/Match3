using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Components
{
    public struct GameConfigTag : IComponentData { }
    
    public struct GridConfig : IComponentData
    {
        public int Width;
        public int Height;

        public readonly int CellCount => Width * Height;
        public readonly int GetIndex(int x, int y) => y * Width + x;
        public readonly int GetIndex(int2 pos) => GetIndex(pos.x, pos.y);
        public readonly int2 GetPos(int index) => new(index % Width, index / Width);
        public readonly bool IsValidPos(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public readonly bool IsValidPos(int2 pos) => IsValidPos(pos.x, pos.y);
    }

    public struct MatchConfig : IComponentData
    {
        public int MatchCount;
        public int PointsPerTile;
    }

    public struct TimingConfig : IComponentData
    {
        public float SwapDuration;
        public float FallDuration;
        public float MatchDelay;
    }
}
