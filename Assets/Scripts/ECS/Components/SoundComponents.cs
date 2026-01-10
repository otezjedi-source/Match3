using Unity.Entities;

namespace Match3.ECS.Components
{
    public enum SoundType : byte
    {
        None = 0,
        Swap = 1,
        Match = 2,
        Drop = 3,
        BtnClick = 4,
    }

    public struct PlaySoundRequest : IComponentData
    {
        public SoundType Type;
    }
}
