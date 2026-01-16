using Unity.Entities;
using Unity.Mathematics;

namespace Match3.ECS.Components
{
    /// <summary>
    /// Game loop phases. Transitions are managed by various systems.
    /// See README.md for the full state machine diagram.
    /// </summary>
    public enum GamePhase : byte
    {
        Idle = 0,       // Waiting for player input
        Swap = 1,       // Swap animation playing
        Match = 2,      // Checking for matches
        Clear = 3,      // Clear animation playing
        Fall = 4,       // Tiles falling to fill gaps
        Fill = 5,       // Spawning new tiles
        GameOver = 6,   // No more valid moves
    }

    /// <summary>
    /// Tag for the game state singleton entity.
    /// </summary>
    public struct GameStateTag : IComponentData { }

    /// <summary>
    /// Current game state. Single instance exists throughout gameplay.
    /// </summary>
    public struct GameState : IComponentData
    {
        public GamePhase Phase;
        public float PhaseTimer; // Used for delays (e.g., before clearing)
    }

    /// <summary>
    /// Player's swap input. Created by InputController, processed by SwapSystem.
    /// </summary>
    public struct PlayerSwapRequest : IComponentData
    {
        public int2 PosA;
        public int2 PosB;
    }

    /// <summary>
    /// Active swap operation. Tracks tiles being swapped for potential revert.
    /// </summary>
    public struct SwapRequest : IComponentData
    {
        public Entity TileA;
        public Entity TileB;
        public int2 PosA;           // Original position of TileA
        public int2 PosB;           // Original position of TileB
        public bool IsReverting;    // True if no match found, swapping back
    }

    /// <summary>
    /// Score event. Created when tiles are cleared, processed by ScoreSyncSystem.
    /// </summary>
    public struct ScoreEvent : IComponentData
    {
        public int Points;
    }

    /// <summary>
    /// One-shot event indicating game over. Created by PossibleMovesSystem.
    /// </summary>
    public struct GameOverEvent : IComponentData { }
}
