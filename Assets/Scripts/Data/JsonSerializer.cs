using Match3.Interfaces;
using Newtonsoft.Json;

namespace Match3.Data
{
    public class JsonSerializer : ISerializer
    {
        public string Serialize<T>(T data) where T : class
        {
            return JsonConvert.SerializeObject(data);
        }

        public T Deserialize<T>(string raw) where T : class
        {
            return JsonConvert.DeserializeObject<T>(raw);
        }
    }
}