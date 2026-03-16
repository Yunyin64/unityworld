using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 选项定义：故事事件中的可选择项
    /// 本质是一个迷你 StoryDefine——被选中时同样执行 Conditions+Effects
    /// 支持反向注入：可声明自己要出现在哪些 StoryDefine 中
    /// </summary>
    public class OptionDefine : StoryBaseDefine
    {
        /// <summary>选项展示文本</summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        /// <summary>
        /// 反向持有：声明此选项要注入到哪些 StoryDefine 中
        /// 这些 StoryDefine 在触发时会自动包含此选项（双向持有合并）
        /// </summary>
        [JsonPropertyName("storyIds")]
        public List<string> StoryIds { get; set; } = [];
    }
}
