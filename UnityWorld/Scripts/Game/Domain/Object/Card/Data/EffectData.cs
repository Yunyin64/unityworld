namespace UnityWorld.Game.Domain.Card
{
    /// <summary>
    /// Effect 运行时数据
    /// 由一个 Trigger + 一个 Condition + 若干 Action 组合而成
    /// </summary>
    public class EffectData
    {
        /// <summary>所用 Trigger 的 Define ID</summary>
        public string TriggerId { get; set; } = "";

        /// <summary>所用 Condition 的 Define ID</summary>
        public string ConditionId { get; set; } = "";

        /// <summary>所用 Action 的 Define ID 列表</summary>
        public List<string> ActionIds { get; set; } = [];

        /// <summary>
        /// 合并后的 TagBag（所有节点 Tags 拼接，重复表示浓度）
        /// 自动涌现，不需要手动维护
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>强度总分 = Trigger.Score + Condition.Score + Σ Action.Score</summary>
        public float PowerScore { get; set; } = 0;

        /// <summary>复杂度总分 = Σ |节点Score| 绝对值</summary>
        public float ComplexityScore { get; set; } = 0;
    }
}
