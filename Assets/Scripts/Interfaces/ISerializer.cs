namespace Match3.Interfaces
{
    /// <summary>
    /// Interface for data serialization/deserialization
    /// Can be json, binary or some other implementation
    /// </summary>
    public interface ISerializer
    {
        string Serialize<T>(T data) where T : class;
        T Deserialize<T>(string raw) where T : class;
    }
}