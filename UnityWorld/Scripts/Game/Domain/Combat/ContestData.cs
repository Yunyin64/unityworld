using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 卡牌完成扫描后构造的待发拼点，入槽前由持有者的拼点 Buff 修正。
    /// </summary>
    public class ContestData:IDomainDataBase
    {
        /// <summary>发起拼点的回放动作编号</summary>
        internal long? PresentationActionId { get; set; }
        /// <summary>关联的回放拼点编号</summary>
        internal long? PresentationContestId { get; set; }
        /// <summary>拼点类型（Zhan/Ci/Da/SheJi/Shield/Block/Dodge）</summary>
        public ContestType ContestType { get; set; } = ContestType.Zhan;

        /// <summary>拼点数值（攻击值/盾值/防值）</summary>
        public long ContestValue { get; set; }

        /// <summary>拼点阶段结束后使用的点数。Lua 回调之间允许出现负数和超出 int 的中间结果。</summary>
        public int FinalValue => (int)Math.Clamp(ContestValue, 0, int.MaxValue);

        /// <summary>元素类型（仅攻击类 Zhan/Ci/Da/SheJi 有效）</summary>
        public ElementType Element { get; set; } = ElementType.None;

        /// <summary>来源卡牌状态（用于获取持有者及卡牌信息）</summary>
        public CombatCard SourceCard { get; set; }

        /// <summary>来源 NPC</summary>
        public CombatNpc OwnerNpc => SourceCard.Owner;

        /// <summary>是否为攻击类拼点类型（Zhan/Ci/Da/SheJi）</summary>
        public bool IsAttackType => ContestType is ContestType.Zhan or ContestType.Ci
            or ContestType.Da or ContestType.SheJi;

        /// <summary>是否为防御类拼点类型（Shield/Block/Dodge）</summary>
        public bool IsDefenseType => ContestType is ContestType.Shield or ContestType.Block
            or ContestType.Dodge;

        /// <summary>记录拼点的日志摘要</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg(ToString());
        }
        
        /// <summary>浅复制当前拼点数据</summary>
        public ContestData Clone()
        {
            var copy = (ContestData)MemberwiseClone();
            return copy;
        }
        /// <summary>通过领域数据接口浅复制拼点</summary>
        IDomainDataBase IDomainDataBase.Clone() => Clone();
        /// <summary>把攻击拼点类型转换为伤害类型</summary>
        public DamageType ToDamageType()
        {
            switch (ContestType)
            {
                case ContestType.Zhan: return DamageType.Zhan;
                case ContestType.Ci: return DamageType.Ci;
                case ContestType.Da: return DamageType.Da;
                case ContestType.SheJi: return DamageType.SheJi;
            }
            return DamageType.None;
        }

        /// <summary>格式化来源卡、拼点类型、元素和点数</summary>
        public override string ToString()
        {
            // 简化格式：CardName|拼点类型|元素|数值
            string cardName = SourceCard?.DisplayName ?? ContestType.ToString();
            string element = Element.Equals(ElementType.None) ? "无" : Element.ToString();
            return $"{cardName}|{ContestType}|{element}|{ContestValue:F0}";
        }
    }
}
