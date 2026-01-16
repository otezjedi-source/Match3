using Unity.Entities;

namespace Match3.ECS.Components
{
    /// <summary>
    /// Sound effect types. Mapped to AudioClips in SoundController.
    /// </summary>
    public enum SoundType : byte
    {
        None = 0,
        Swap = 1,
        Match = 2,
        Drop = 3,
        BtnClick = 4,
    }

    /// <summary>
    /// Request to play a sound. Created by ECS systems, processed by SoundSyncSystem.
    /// Allows ECS logic to trigger audio without direct controller access.
    /// </summary>
    public struct PlaySoundRequest : IComponentData
    {
        public SoundType Type;
    }
}
