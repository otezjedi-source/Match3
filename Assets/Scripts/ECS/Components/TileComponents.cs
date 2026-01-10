using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Components
{
    public enum TileType : byte
    {
        None = 0,
        Red = 1,
        Green = 2,
        Blue = 3,
        Yellow = 4,
        Purple = 5,
    }

    public enum TileState : byte
    {
        Idle = 0,
        Swap = 1,
        Clear = 2,
        Fall = 3,
    }

    public struct TileData : IComponentData
    {
        public TileType Type;
        public int2 GridPos;
    }

    public struct TileStateData : IComponentData
    {
        public TileState State;
    }

    public struct TileWorldPos : IComponentData
    {
        public float3 Pos;
    }

    public struct TileMove : IComponentData, IEnableableComponent
    {
        public float3 StartPos;
        public float3 TargetPos;
        public float Duration;
        public float Elapsed;
    }

    public struct MatchTag : IComponentData { }

    public struct ClearTag : IComponentData { }
    public struct ClearDoneEvent : IComponentData { }

    public struct DropTag : IComponentData { }
    public struct DropDoneEvent : IComponentData { }
}
