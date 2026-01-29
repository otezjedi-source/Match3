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
        public GamePhase phase;
        public float phaseTimer; // Used for delays (e.g., before clearing)
    }

    /// <summary>
    /// Player's swap input. Created by InputController, processed by SwapSystem.
    /// </summary>
    public struct PlayerSwapRequest : IComponentData
    {
        public int2 posA;
        public int2 posB;
    }

    /// <summary>
    /// Active swap operation. Tracks tiles being swapped for potential revert.
    /// </summary>
    public struct SwapRequest : IComponentData
    {
        public Entity tileA;
        public Entity tileB;
        public int2 posA;           // Original position of TileA
        public int2 posB;           // Original position of TileB
        public bool isReverting;    // True if no match found, swapping back
        public bool isHorizontal;   // True if swap is horizontal. Used for line bonus direction.
    }

    /// <summary>
    /// Score event. Created when tiles are cleared, processed by ScoreSyncSystem.
    /// </summary>
    public struct ScoreEvent : IComponentData
    {
        public int points;
    }

    /// <summary>
    /// One-shot event indicating game over. Created by PossibleMovesSystem.
    /// </summary>
    public struct GameOverEvent : IComponentData { }
}
