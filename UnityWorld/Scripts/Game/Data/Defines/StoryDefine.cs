using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 故事事件定义：代表世界中可触发的一个故事/事件
    /// IsHide=true 为隐形事件（自动执行，不展示UI）
    /// IsHide=false 为显示事件（弹出事件窗口，玩家/NPC做选择）
    /// </summary>
    public class StoryDefine : StoryBaseDefine
    {
        /// <summary>
        /// 是否为隐形事件
        /// true：自动执行 Conditions+Effects，不显示任何UI
        /// false：显示事件弹窗，需要选项交互
        /// </summary>
        [JsonPropertyName("isHide")]
        public bool IsHide { get; set; } = true;

        /// <summary>事件标题（IsHide=false 时必填）</summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>事件正文描述（IsHide=false 时必填）</summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// 正向持有的选项 ID 列表
        /// 最终选项 = 此列表 + 所有反向注入此 Story 的 OptionDefine（双向持有合并）
        /// </summary>
        [JsonPropertyName("optionIds")]
        public List<string> OptionIds { get; set; } = [];

        /// <summary>运行时合并后的完整选项 ID 列表（由 StoryDefineMgr.Begin() 构建）</summary>
        [JsonIgnore]
        public List<string> MergedOptionIds { get; set; } = [];
    }
}
