using Newtonsoft.Json;

namespace Match3.Data
{
    /// <summary>
    /// Serializable save data model. Add new fields as needed.
    /// Uses Newtonsoft.Json for serialization.
    /// </summary>
    public class SaveData
    {
        public int HighScore { get; set; }

        /// <summary>
        /// Deserialize from JSON. Returns empty SaveData on failure.
        /// </summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new();

            try
            {
                return JsonConvert.DeserializeObject<SaveData>(json) ?? new();
            }
            catch
            {
                return new();
            }
        }

        public string ToJson() => JsonConvert.SerializeObject(this);
    }
}
