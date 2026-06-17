using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 行为卡定义：个体持有的可主动使用的行为意图卡
    /// 使用时创建 Behavior 实例并填入 NPC 行为槽，在行为生命周期中触发 Story
    /// </summary>
    public class BehaviorCardDefine : DefineBase
    {

        // ── 行为配置 ───────────────────────────────────────────────────────

        /// <summary>
        /// 行为 ID：决定使用卡后创建的行为类型
        /// 留空或 "None" 表示瞬时卡（无行为，直接触发 OnStart Story）
        /// 官方 ID：Move/Practice/Explore/Social 等，其他从 ExtraBehaviorDefine 加载
        /// </summary>
        [JsonPropertyName("BehaviorId")]
        public string BehaviorId { get; set; } = "";

        /// <summary>
        /// 行为持续时间（秒），0 表示无限持续（需手动打断）
        /// </summary>
        [JsonPropertyName("BehaviorDuration")]
        public float BehaviorDuration { get; set; } = 0f;

        /// <summary>
        /// 是否为主行为（true=填入主槽，false=填入次要列表）
        /// </summary>
        [JsonPropertyName("BehaviorIsPrimary")]
        public bool BehaviorIsPrimary { get; set; } = true;

        /// <summary>使用后是否消耗（true=消耗并移除，false=保留可重复使用）</summary>
        [JsonPropertyName("IsConsumable")]
        public bool IsConsumable { get; set; } = false;

        // ── 生命周期 Story 触发规则 ───────────────────────────────────────

        /// <summary>行为开始时触发的 Story 规则</summary>
        [JsonPropertyName("OnStart")]
        public StoryTriggerRule OnStart { get; set; }

        /// <summary>行为结束时触发的 Story 规则</summary>
        [JsonPropertyName("OnEnd")]
        public StoryTriggerRule OnEnd { get; set; }

        /// <summary>行为被打断时触发的 Story 规则</summary>
        [JsonPropertyName("OnInterrupt")]
        public StoryTriggerRule OnInterrupt { get; set; }

        /// <summary>行为每 Tick 触发的 Story 规则（概率触发）</summary>
        [JsonPropertyName("OnTick")]
        public StoryTickRule OnTick { get; set; }

        /// <summary>行为计时触发规则（延迟触发）</summary>
        [JsonPropertyName("OnTimer")]
        public StoryTimerRule OnTimer { get; set; }

        // ── 兼容旧数据：将 storyIds/storyTags 映射为 OnStart ──────────────

        /// <summary>
        /// [已废弃] 直接指定触发的 StoryDefine ID 列表
        /// 兼容旧 JSON，自动迁移至 OnStart.StoryIds
        /// </summary>
        [JsonPropertyName("storyIds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> LegacyStoryIds
        {
            get => null; // 序列化时不输出
            set
            {
                if (value != null && value.Count > 0)
                {
                    OnStart ??= new StoryTriggerRule();
                    OnStart.StoryIds = value;
                }
            }
        }

        /// <summary>
        /// [已废弃] TagBag 动态匹配用 Tag 列表
        /// 兼容旧 JSON，自动迁移至 OnStart.StoryTags
        /// </summary>
        [JsonPropertyName("storyTags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> LegacyStoryTags
        {
            get => null; // 序列化时不输出
            set
            {
                if (value != null && value.Count > 0)
                {
                    OnStart ??= new StoryTriggerRule();
                    OnStart.StoryTags = value;
                }
            }
        }
    }

    /// <summary>
    /// Story 触发规则：直接 StoryId 或 TagBag 动态匹配
    /// </summary>
    public class StoryTriggerRule
    {
        /// <summary>
        /// 直接指定触发的 StoryDefine ID 列表（确定性模式）
        /// 优先级高于 StoryTags，不为空时从此列表随机选一个触发
        /// </summary>
        [JsonPropertyName("StoryIds")]
        public List<string> StoryIds { get; set; }

        /// <summary>
        /// TagBag 动态匹配用 Tag 列表（涌现性模式）
        /// 仅当 StoryIds 为空时生效，在全局 StoryPool 中做 TagBag 匹配后触发
        /// </summary>
        [JsonPropertyName("StoryTags")]
        public List<string> StoryTags { get; set; }
    }

    /// <summary>
    /// OnTick Story 触发规则：每 Tick 有概率触发
    /// </summary>
    public class StoryTickRule
    {
        /// <summary>每 Tick 触发概率 (0~1)</summary>
        [JsonPropertyName("Chance")]
        public float Chance { get; set; } = 0.1f;

        /// <summary>触发的 Story 规则</summary>
        [JsonPropertyName("Trigger")]
        public StoryTriggerRule Trigger { get; set; }
    }

    /// <summary>
    /// OnTimer Story 触发规则：延迟触发（一次性）
    /// </summary>
    public class StoryTimerRule
    {
        /// <summary>延迟时间（秒）</summary>
        [JsonPropertyName("Delay")]
        public float Delay { get; set; } = 10f;

        /// <summary>触发的 Story 规则</summary>
        [JsonPropertyName("Trigger")]
        public StoryTriggerRule Trigger { get; set; }
    }
}
