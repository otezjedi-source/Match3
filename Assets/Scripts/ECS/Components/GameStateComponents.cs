using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Components
{
    public enum GamePhase : byte
    {
        Idle = 0,
        Swap = 1,
        Match = 2,
        Clear = 3,
        Fall = 4,
        Fill = 5,
        GameOver = 6,
    }

    public struct GameStateTag : IComponentData { }

    public struct GameState : IComponentData
    {
        public GamePhase Phase;
        public float PhaseTimer;
    }

    public struct PlayerSwapRequest : IComponentData
    {
        public int2 PosA;
        public int2 PosB;
    }

    public struct SwapRequest : IComponentData
    {
        public Entity TileA;
        public Entity TileB;
        public int2 PosA;
        public int2 PosB;
        public bool IsReverting;
    }

    public struct ScoreEvent : IComponentData
    {
        public int Points;
    }

    public struct GameOverEvent : IComponentData { }
}
