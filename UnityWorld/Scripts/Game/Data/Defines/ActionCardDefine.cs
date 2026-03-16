using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 行为卡定义：个体持有的可主动使用的行为意图卡
    /// 使用时触发对应的 StoryDefine（直接指定或 TagBag 动态匹配）
    /// </summary>
    public class ActionCardDefine : DefineBase
    {
        /// <summary>语义标签，用于 TagBag 匹配</summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// 直接指定触发的 StoryDefine ID 列表（确定性模式）
        /// 优先级高于 StoryTags，不为空时从此列表随机选一个触发
        /// </summary>
        [JsonPropertyName("storyIds")]
        public List<string> StoryIds { get; set; } = [];

        /// <summary>
        /// TagBag 动态匹配用 Tag 列表（涌现性模式）
        /// 仅当 StoryIds 为空时生效，在全局 StoryPool 中做 TagBag 匹配后触发
        /// </summary>
        [JsonPropertyName("storyTags")]
        public List<string> StoryTags { get; set; } = [];

        /// <summary>使用后是否消耗（true=消耗并移除，false=保留可重复使用）</summary>
        [JsonPropertyName("isConsumable")]
        public bool IsConsumable { get; set; } = false;
    }
}
