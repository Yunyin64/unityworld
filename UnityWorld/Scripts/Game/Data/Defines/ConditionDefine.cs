using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Condition（触发条件检查）定义
    /// 描述一个Effect触发时需满足的额外条件，score越低代表条件越苛刻
    /// </summary>
    public class ConditionDefine : DefineBase
    {
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        /// <summary>强度分（0或负数，越苛刻越低）</summary>
        [JsonPropertyName("score")]
        public int Score { get; set; } = 0;

        /// <summary>Tag列表（重复表示浓度）</summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = [];

        /// <summary>冲突Tag列表</summary>
        [JsonPropertyName("conflictTags")]
        public List<string> ConflictTags { get; set; } = [];

        /// <summary>稀有度</summary>
        [JsonPropertyName("rarity")]
        public int Rarity { get; set; } = 0;
    }
}
