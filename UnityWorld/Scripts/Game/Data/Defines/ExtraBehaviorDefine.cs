using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 行为拓展定义：数据驱动定义行为变体
    /// 继承 DefineBase，ID 即为 BehaviorId
    /// </summary>
    public class ExtraBehaviorDefine : DefineBase
    {
        /// <summary>描述文本</summary>
        [JsonPropertyName("Desc")]
        public string Desc { get; set; } = "";

        /// <summary>语义标签列表</summary>
        [JsonPropertyName("Tags")]
        public List<string> Tags { get; set; } = new();
    }
}
