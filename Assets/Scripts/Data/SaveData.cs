using Newtonsoft.Json;

namespace MiniIT.SAVE
{
    public class SaveData
    {
        public int HighScore { get; set; }

        public static SaveData FromJson(string json)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<SaveData>(json);
                return new SaveData { HighScore = data?.HighScore ?? 0 };
            }
            catch
            {
                return new SaveData();
            }
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
