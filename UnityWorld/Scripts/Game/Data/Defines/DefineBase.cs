

using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    public class DefineBase
    {

        [JsonPropertyName("ID")]
        public string ID { get; set; } = "";
        [JsonPropertyName("DisplayName")]
        public string DisplayName { get; set; } = "";
    }
}