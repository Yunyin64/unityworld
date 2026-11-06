using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard
    {
        // 蓄灵属于单张战斗卡实例，不进入世界卡数据，也不由背包重复保存。
        private readonly List<ElementType> _assignedMana = new();

        /// <summary>当前已接收的灵元；对外只读，转移统一由背包协调。</summary>
        public IReadOnlyList<ElementType> AssignedElements => _assignedMana.AsReadOnly();

        /// <summary>当前点亮份数，不包含仍在背包格上的灵元。</summary>
        public int LitCount => _assignedMana.Count;

        /// <summary>只接收仍缺少的匹配元素；冻结不阻止蓄灵，待发期间不接收下一次资源。</summary>
        public bool CanAcceptMana(ElementType element)
        {
            return BaseData.HasActiveAbility && !CheckPhase(CombatCardPhase.InPending) &&
                _assignedMana.Count < GetSize() && GetManaCost().TryGetValue(element, out var required) &&
                _assignedMana.Count(e => e.Equals(element)) < required;
        }

        /// <summary>检查本卡是否已按元素需求满灵，不执行支付或改变状态。</summary>
        public bool HasActivationResources() => BaseData.HasActiveAbility &&
            _assignedMana.Count == GetSize() &&
            GetManaCost().All(cost => _assignedMana.Count(e => e.Equals(cost.Key)) == cost.Value);

        /// <summary>背包转移的接收端；无回调，失败不改变本卡状态或源格。</summary>
        internal bool TryReceiveMana(ElementType element)
        {
            if (!CanAcceptMana(element)) return false;
            _assignedMana.Add(element);
            return true;
        }

        /// <summary>成功使用或离场后清空蓄灵；重新入场从空状态开始，不再额外支付灵元。</summary>
        internal void ClearAssignedMana() => _assignedMana.Clear();
    }
}
