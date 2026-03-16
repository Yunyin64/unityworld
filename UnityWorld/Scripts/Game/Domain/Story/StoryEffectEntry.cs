using System.Text.Json.Serialization;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 故事效果条目：对应配置中 Effects 数组的单个条目
    /// 包含要调用的原子函数名和参数列表
    /// </summary>
    public class StoryEffectEntry
    {
        /// <summary>要调用的 StoryEffectFunc 注册表中的函数名</summary>
        [JsonPropertyName("funcName")]
        public string FuncName { get; set; } = "";

        /// <summary>传递给函数的参数列表（字符串形式，由各函数自行解析）</summary>
        [JsonPropertyName("args")]
        public List<string> Args { get; set; } = [];
    }
}
