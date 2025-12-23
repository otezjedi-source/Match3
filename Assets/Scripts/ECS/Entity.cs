namespace MiniIT.ECS
{
    public struct Entity
    {
        public int Id { get; }

        public Entity(int id)
        {
            Id = id;
        }

        public static Entity Null => new Entity(-1);

        public bool IsNull => Id < 0;

        public override bool Equals(object obj)
        {
            if (obj is Entity other)
                return Id == other.Id;
            return false;
        }

        public override int GetHashCode() => Id;

        public static bool operator ==(Entity a, Entity b) => a.Id == b.Id;
        public static bool operator !=(Entity a, Entity b) => a.Id != b.Id;
    }
}
