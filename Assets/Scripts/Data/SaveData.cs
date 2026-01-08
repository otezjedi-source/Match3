using Newtonsoft.Json;

namespace Match3.Save
{
    public class SaveData
    {
        public int HighScore { get; set; }

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
