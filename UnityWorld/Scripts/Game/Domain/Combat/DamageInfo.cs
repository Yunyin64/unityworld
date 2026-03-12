namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 伤害信息：描述一次伤害事件的完整参数。
    /// 只描述"将要发生什么"，不记录结算结果。
    /// </summary>
    public class DamageInfo
    {
        // ── 来源 / 目标 ──────────────────────────────────

        /// <summary>伤害来源的 NpcId（施害方，可为 null 表示环境伤害）</summary>
        public string? SourceId { get; set; }

        /// <summary>伤害目标的 NpcId（受害方）</summary>
        public string TargetId { get; set; } = string.Empty;

        // ── 伤害数值 ──────────────────────────────────────

        /// <summary>基础伤害值（结算前的原始值）</summary>
        public float BaseDamage { get; set; }

        // ── 伤害分类 ──────────────────────────────────────

        /// <summary>伤害类型：物理 / 魔法 / 真实</summary>
        public DamageType DamageType { get; set; } = DamageType.Physical;

        /// <summary>元素类型：无 / 火 / 冰 / 雷 / 毒 / 暗 / 光</summary>
        public ElementType ElementType { get; set; } = ElementType.None;

        // ── 来源标记（可选，便于后续扩展）─────────────────

        /// <summary>触发此伤害的卡牌/技能 Id（可为 null）</summary>
        public string? SkillId { get; set; }

        // ── 构造 ──────────────────────────────────────────

        public DamageInfo() { }

        public DamageInfo(string targetId, float baseDamage,
                          ElementType elementType,
                          DamageType damageType = DamageType.Physical,
                          string? sourceId = null,
                          string? skillId = null)
        {
            TargetId    = targetId;
            BaseDamage  = baseDamage;
            DamageType  = damageType;
            ElementType = elementType;
            SourceId    = sourceId;
            SkillId     = skillId;
        }
    }
}
