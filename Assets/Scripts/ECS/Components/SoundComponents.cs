using Unity.Entities;

namespace Match3.ECS.Components
{
    public enum SoundType : byte
    {
        None = 0,
        Match = 1,
        Drop = 2,
        BtnClick = 3,
    }

    public struct PlaySoundRequest : IComponentData
    {
        public SoundType Type;
    }
}
