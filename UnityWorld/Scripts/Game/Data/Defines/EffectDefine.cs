using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Effect 定义：一个手配的 Trigger + Condition + List&lt;Action&gt; 组合实例。
    /// 策划可以直接配置典型的 Effect，供 CardDefine 引用。
    /// 也可以作为生成系统的输出结果被缓存使用。
    /// </summary>
    public class EffectDefine : DefineBase
    {
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        /// <summary>触发器节点 ID（引用 TriggerDefine）</summary>
        [JsonPropertyName("triggerId")]
        public string TriggerId { get; set; } = "";

        /// <summary>条件节点 ID（引用 ConditionDefine，空字符串表示无条件）</summary>
        [JsonPropertyName("conditionId")]
        public string ConditionId { get; set; } = "";

        /// <summary>动作节点 ID 列表（引用 ActionDefine，顺序有意义）</summary>
        [JsonPropertyName("actionIds")]
        public List<string> ActionIds { get; set; } = [];

        /// <summary>
        /// 强度分缓存（= Trigger.Score + Condition.Score + Σ Action.Score）
        /// 手配时可不填，由系统运行时计算校验；填了则作为快速读取缓存
        /// </summary>
        [JsonPropertyName("powerScore")]
        public int PowerScore { get; set; } = 0;

        /// <summary>
        /// 复杂度分缓存（= Σ |Score| of all nodes）
        /// 同上，可不填
        /// </summary>
        [JsonPropertyName("complexityScore")]
        public int ComplexityScore { get; set; } = 0;
    }
}
