using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 伤害信息：描述一次伤害事件的完整参数。
    /// 只描述"将要发生什么"，不记录结算结果。
    /// </summary>
    public class DamageInfo:ContextBase
    {
        /// <summary>伤害关联的回放来源</summary>
        internal PresentationOrigin PresentationOrigin { get; set; }
        // ── 来源 / 目标 ──────────────────────────────────
        /// <summary>伤害来源角色</summary>
        public CombatNpc SourceNpc { get; set; }
        /// <summary>伤害来源卡牌，可为空</summary>
        public CombatCard SourceCard { get; set; }
        /// <summary>实际承伤的角色</summary>
        public CombatNpc TargetNpc { get; set; }

        // ── 伤害数值 ──────────────────────────────────────

        /// <summary>基础伤害值（结算前的原始值）</summary>
        // Lua 在同一阶段连续改值，暂存相加的中间结果，阶段结束才限制到战斗点数范围。
        public long Damage { get; set; }
        /// <summary>伤害的物理类型</summary>
        public DamageType damageType { get; set; } = DamageType.Zhan;

        /// <summary>元素类型：无元素、五行或扩展元素</summary>
        public ElementType ElementType { get; set; } = ElementType.None;

        // ── 来源标记（可选，便于后续扩展）─────────────────

        /// <summary>创建空的伤害上下文</summary>

        // ── 构造 ──────────────────────────────────────────
        public DamageInfo(){}
        
        /// <summary>从拼点复制来源、目标和伤害类型</summary>
        public DamageInfo(ContestData data)
        {
            SourceNpc = data.OwnerNpc;
            SourceCard = data.SourceCard;
            TargetNpc = data.OwnerNpc.GetTarget();
            damageType = data.ToDamageType();
            ElementType = data.Element;
            PresentationOrigin = new PresentationOrigin(data.PresentationActionId, null, data.PresentationContestId, null);

        }
        /// <summary>按指定来源、目标、数值和类型构造伤害</summary>
        public DamageInfo(CombatNpc source, CombatCard sourceCard, CombatNpc target,
            float damage, DamageType damageType = DamageType.Zhan,
            ElementType? elementType = null)
        {
            SourceNpc = source;
            SourceCard = sourceCard;
            TargetNpc = target;
            Damage = damage.ToInt();
            this.damageType = damageType;
            ElementType = elementType ?? ElementType.None;
        }
    }
}
