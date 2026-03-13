using System.Text.Json.Serialization;

    public class DefineBase
    {

        [JsonPropertyName("ID")]
        public string ID { get; set; } = "";
        [JsonPropertyName("DisplayName")]
        public string DisplayName { get; set; } = "";
    }