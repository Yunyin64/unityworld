using System.Text.Json.Serialization;

    public class DefineBase
    {

        [JsonPropertyName("ID")]
        public string ID { get; set; } = "";
        [JsonPropertyName("DisplayName")]
        public string DisplayName { get; set; } = "";

        public List<string> Tags { get; set;} = new();
    }

    
    /// <summary>
    /// 参数定义：描述一个可选参数的名称、类型、可选值和对应强度评分
    /// </summary>
    public class APIParamDef
    {
        /// <summary>参数名</summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; } = "";

        /// <summary>参数类型：String / Int / Float / Bool</summary>
        [JsonPropertyName("Type")]
        public string Type { get; set; } = "String";

        /// <summary>可选值列表（用JsonElement兼容多类型）</summary>
        [JsonPropertyName("Value")]
        public List<System.Text.Json.JsonElement> Value { get; set; } = [];

        /// <summary>每个可选值对应的强度评分</summary>
        [JsonPropertyName("Score")]
        public List<float> Score { get; set; } = [0];

        /// <summary>每个可选值对应的强度乘数评分</summary>
        [JsonPropertyName("Multiplier")]
        public List<float> Multiplier { get; set; } = [1];
    }