using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Action（效果动作）定义
    /// 是Effect中实际产生效果的部分，score为正数，消耗Effect的强度预算
    /// </summary>
    public class ActionDefine : DefineBase
    {
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        /// <summary>强度分（正数，消耗Effect预算）</summary>
        [JsonPropertyName("score")]
        public float Score { get; set; } = 0;
        
        /// <summary>稀有度</summary>
        [JsonPropertyName("weight")]
        public float Weight { get; set; } = 1;

        /// <summary>Tag列表（这里不可重复）</summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = [];

        /// <summary>冲突Tag列表</summary>
        [JsonPropertyName("conflictTags")]
        public List<string> ConflictTags { get; set; } = [];

    }
}
