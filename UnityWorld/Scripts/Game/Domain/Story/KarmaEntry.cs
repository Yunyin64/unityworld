namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 劫缘池条目：带权重和条件的故事触发配置
    /// </summary>
    public class KarmaEntry
    {
        /// <summary>要触发的 StoryDefine ID</summary>
        public string StoryId { get; set; } = "";

        /// <summary>触发权重（相对权重，越大越容易被随机到）</summary>
        public float Weight { get; set; } = 1f;

        /// <summary>触发前置条件（AND 逻辑，全部满足才进入候选池）</summary>
        public List<StoryCondition> Conditions { get; set; } = [];

        public KarmaEntry() { }

        public KarmaEntry(string storyId, float weight, List<StoryCondition>? conditions = null)
        {
            StoryId    = storyId;
            Weight     = weight;
            Conditions = conditions ?? [];
        }
    }
}
