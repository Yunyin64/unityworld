using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 故事基础定义：StoryDefine 和 OptionDefine 的公共基类
    /// 包含 Tags、Conditions、Effects、LuaScript 等共享字段
    /// </summary>
    public class StoryBaseDefine : DefineBase
    {
        /// <summary>语义标签列表，用于 TagBag 匹配（可重复，重复次数代表浓度）</summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = [];

        /// <summary>触发/出现条件列表（AND 逻辑，全部满足才通过）</summary>
        [JsonPropertyName("conditions")]
        public List<UnityWorld.Game.Domain.StoryCondition> Conditions { get; set; } = [];

        /// <summary>触发后执行的效果列表（简单轨）</summary>
        [JsonPropertyName("effects")]
        public List<UnityWorld.Game.Domain.StoryEffectEntry> Effects { get; set; } = [];

        /// <summary>
        /// 复杂轨 Lua 脚本路径（可为 null）
        /// 当前版本预留，非空时打 Warning 并降级到简单轨
        /// </summary>
        [JsonPropertyName("luaScript")]
        public string? LuaScript { get; set; }

        /// <summary>检查所有 Conditions 是否全部满足</summary>
        public bool EvaluateConditions(UnityWorld.Game.Domain.StoryContext ctx)
        {
            foreach (var cond in Conditions)
            {
                if (!cond.Evaluate(ctx)) return false;
            }
            return true;
        }
    }
}
