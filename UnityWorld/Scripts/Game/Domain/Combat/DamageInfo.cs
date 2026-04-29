namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 伤害信息：描述一次伤害事件的完整参数。
    /// 只描述"将要发生什么"，不记录结算结果。
    /// </summary>
    public class DamageInfo:ContextBase
    {
        // ── 来源 / 目标 ──────────────────────────────────
        public CombatNpc SourceNpc { get; set; }
        public CombatCard SourceCard { get; set; }
        public CombatNpc TargetNpc { get; set; }

        // ── 伤害数值 ──────────────────────────────────────

        /// <summary>基础伤害值（结算前的原始值）</summary>
        public float Damage { get; set; } = 0;
        public DamageType damageType { get; set; } = DamageType.Zhan;

        /// <summary>元素类型：无 / 火 / 冰 / 雷 / 毒 / 暗 / 光</summary>
        public ElementType ElementType { get; set; } = ElementType.None;

        // ── 来源标记（可选，便于后续扩展）─────────────────

        /// <summary>触发此伤害的卡牌/技能 Id（可为 null）</summary>

        // ── 构造 ──────────────────────────────────────────
        public DamageInfo(){}
        
        public DamageInfo(ContestData data)
        {
            SourceNpc = data.OwnerNpc;
            SourceCard = data.SourceCard;
            TargetNpc = data.OwnerNpc.Target;
            damageType = data.ToDamageType();
            ElementType = data.Element;

        }
        public DamageInfo(CombatNpc source, CombatCard sourceCard, CombatNpc target,
            float damage, DamageType damageType = DamageType.Zhan,
            ElementType? elementType = null)
        {
            SourceNpc = source;
            SourceCard = sourceCard;
            TargetNpc = target;
            Damage = damage;
            this.damageType = damageType;
            ElementType = elementType ?? ElementType.None;
        }
    }
}
