using System.Linq;
using System.Text.Json.Serialization;
using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 战斗修正静态配置模板。
    /// 描述一种战斗修正（Buff/Debuff）的属性修正、触发机制及特殊实现。
    /// 运行时由 <see cref="CombatNpcModifierDefineMgr"/> 加载，通过 <see cref="CreateModifier"/> 实例化为 <see cref="CombatNpcModifier"/>
    /// 挂载到战斗 NPC 上。
    /// </summary>
    public class CombatNpcModifierDefine : DefineBase
    {
        // ── A. 生命周期 ──────────────────────────────────────

        /// <summary>
        /// 持续时间（秒）。
        /// -1 = 永久有效；大于 0 = 有限时长，到期后 Modifier 自动移除。
        /// </summary>
        [JsonPropertyName("Duration")]
        public float Duration { get; set; } = -1f;

        /// <summary>最大叠层（0 表示无上限）</summary>
        [JsonPropertyName("MaxStack")]
        public int MaxStack { get; set; }

        /// <summary>叠层时是否刷新剩余时间</summary>
        [JsonPropertyName("RefreshOnStack")]
        public bool RefreshOnStack { get; set; } = true;

        // ── B. 数值修正 ──────────────────────────────────────

        /// <summary>对 StatBlock 的修正列表（Apply 时添加，Remove 时撤销）</summary>
        [JsonPropertyName("StatModifiers")]
        public List<StatModifierEntry> StatModifiers { get; set; } = new();

        // ── C. 触发机制 ──────────────────────────────────────

        /// <summary>触发器定义 ID 列表（复用卡牌 TCA 体系，注册到 EventMgr）</summary>
        [JsonPropertyName("TriggerIds")]
        public List<string> TriggerIds { get; set; } = new();

        // ── D. 特殊实现 ──────────────────────────────────────

        /// <summary>特殊实现标识（外部注册处理器查找键，如 "burn"、"stun"、"transform"）</summary>
        [JsonPropertyName("ImplId")]
        public string ImplId { get; set; } = "";

    }
}
