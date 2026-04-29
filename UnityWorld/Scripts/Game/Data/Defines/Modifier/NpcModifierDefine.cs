using System.Linq;
using System.Text.Json.Serialization;
using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// NPC 修正静态配置模板。
    /// 描述一种大地图阶段的 NPC 修正（Buff/Debuff）的属性修正及叠层规则。
    /// 运行时由 <see cref="NpcModifierDefineMgr"/> 加载，通过 <see cref="CreateModifier"/> 实例化为 <see cref="NpcModifier"/>
    /// 挂载到 NPC 上。
    /// </summary>
    public class NpcModifierDefine : DefineBase
    {
        // ── 生命周期 ──────────────────────────────────────────

        /// <summary>
        /// 持续时间（秒）。
        /// -1 = 永久有效；大于 0 = 有限时长，到期后 Modifier 自动移除。
        /// </summary>
        [JsonPropertyName("Duration")]
        public float Duration { get; set; } = -1f;

        /// <summary>最大叠层（0 表示无上限）</summary>
        [JsonPropertyName("MaxStack")]
        public int MaxStack { get; set; }

        // ── 数值修正 ──────────────────────────────────────────

        /// <summary>对 StatBlock 的修正列表（Apply 时添加，Remove 时撤销）</summary>
        [JsonPropertyName("StatModifiers")]
        public List<StatModifierEntry> StatModifiers { get; set; } = new();

        // ── 工厂方法 ──────────────────────────────────────────

        /// <summary>
        /// 从本定义实例化一个 <see cref="NpcModifier"/>。
        /// </summary>
        /// <param name="sourceId">修正源标识（如 Trait ID、事件 ID 等）</param>
        public NpcModifier CreateModifier(string sourceId)
            => new NpcModifier
            {
                Id = ID,
                SourceId = sourceId,
                Duration = Duration,
                RemainingTime = Duration,
            };
    }
}
