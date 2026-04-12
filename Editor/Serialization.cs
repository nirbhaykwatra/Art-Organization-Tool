using System.IO;
using Newtonsoft.Json;

namespace ArtPipeline
{
    public class Serializer
    {
        public void Serialize<T>(T data, string filePath)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        
        public T Deserialize<T>(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}