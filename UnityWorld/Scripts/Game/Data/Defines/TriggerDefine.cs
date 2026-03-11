using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Trigger（触发条件）定义
    /// 描述一个Effect何时被触发，score越低代表触发条件越苛刻，为Action释放更多预算
    /// </summary>
    public class TriggerDefine : DefineBase
    {
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        /// <summary>强度分（0或负数，越苛刻越低）</summary>
        [JsonPropertyName("score")]
        public int Score { get; set; } = 0;

        /// <summary>Tag列表（重复表示浓度）</summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = [];

        /// <summary>冲突Tag列表（含这些Tag的节点不能与本节点同处一个Effect）</summary>
        [JsonPropertyName("conflictTags")]
        public List<string> ConflictTags { get; set; } = [];

        /// <summary>稀有度（影响被随机选中的概率）</summary>
        [JsonPropertyName("rarity")]
        public int Rarity { get; set; } = 0;
    }
}
