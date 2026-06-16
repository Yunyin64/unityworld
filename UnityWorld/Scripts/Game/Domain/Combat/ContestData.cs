using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 拼点数据快照：卡牌 CD 就绪时构造，塞入待发槽，拼完即丢。
    ///  "临时视图"，固定住拼点数值不受后续 buff 影响。
    /// </summary>
    public class ContestData:IDomainDataBase
    {
        /// <summary>拼点类型（Zhan/Ci/Da/SheJi/Shield/Block）</summary>
        public ContestType ContestType { get; set; } = ContestType.Zhan;

        /// <summary>拼点数值（攻击值/盾值/防值）</summary>
        public int ContestValue { get; set; } = 0;

        /// <summary>元素类型（仅攻击类 Zhan/Ci/Da/SheJi 有效）</summary>
        public ElementType Element { get; set; } = ElementType.None;

        /// <summary>来源卡牌状态（用于重置 CD、获取完整卡信息）</summary>
        public CombatCard SourceCard { get; set; }

        /// <summary>来源 NPC</summary>
        public CombatNpc OwnerNpc => SourceCard.Owner;

        /// <summary>是否为攻击类拼点类型（Zhan/Ci/Da/SheJi）</summary>
        public bool IsAttackType => ContestType is ContestType.Zhan or ContestType.Ci
            or ContestType.Da or ContestType.SheJi;

        /// <summary>是否为防御类拼点类型（Shield/Block/Dodge）</summary>
        public bool IsDefenseType => ContestType is ContestType.Shield or ContestType.Block
            or ContestType.Dodge;

        public void Log()
        {
            LogMgr.Instance.Dbg(ToString());
        }
        
        public ContestData Clone()
        {
            var copy = (ContestData)MemberwiseClone();
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();
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

        public override string ToString()
        {
            // 简化格式：CardName|拼点类型|元素|数值
            string cardName = SourceCard?.DisplayName ?? ContestType.ToString();
            string element = Element.Equals(ElementType.None) ? "无" : Element.ToString();
            return $"{cardName}|{ContestType}|{element}|{ContestValue:F0}";
        }
    }
}
