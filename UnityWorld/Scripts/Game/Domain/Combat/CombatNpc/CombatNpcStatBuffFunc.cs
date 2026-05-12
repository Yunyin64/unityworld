using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// CombatNpc 轻量级属性修正 API（partial）：
    /// 直接对战斗 StatBlock 施加/移除永久属性修正，不走 Define 体系。
    /// </summary>
    public partial class CombatNpc
    {
        // ══════════════════════════════════════════════════════════
        //  AddStatBuff
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 向战斗 StatBlock 施加一个永久属性修正（战斗结束随 Snapshot 清理）。
        /// 默认 Flat 加算；需要其他类型时传 modifierType 参数。
        /// </summary>
        /// <param name="statId">目标属性 ID（如 "HpMax", "Atk", "Def"）</param>
        /// <param name="value">修正值</param>
        /// <param name="modifierType">修正类型字符串："Flat"(默认)/"Percent"/"Override"/"ClampMax"/"ClampMin"</param>
        /// <param name="sourceId">来源标识，用于精准移除；不传时自动生成（将无法主动移除）</param>
        public void AddStatBuff(string statId, float value, ModifierType modifierType =  ModifierType.Flat, string sourceId = null)
        {

            if (string.IsNullOrEmpty(sourceId))
            {
                sourceId = $"StatBuff_{statId}_{modifierType}";
            }

            Stats.AddModifier(statId, new StatModifier(modifierType, value, sourceId));
            Log($"[StatBuff] +{statId} ({modifierType} {value:+0.##;-0.##;0}) source={sourceId}");
        }

        // ══════════════════════════════════════════════════════════
        //  RemoveStatBuff
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 按来源标识移除所有相关属性修正。sourceId 不存在时静默通过。
        /// </summary>
        /// <param name="sourceId">施加时使用的来源标识</param>
        public void RemoveStatBuff(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId)) return;

            Stats.RemoveModifiersBySource(sourceId);
            Log($"[StatBuff] 移除 source={sourceId}");
        }
    }
}
