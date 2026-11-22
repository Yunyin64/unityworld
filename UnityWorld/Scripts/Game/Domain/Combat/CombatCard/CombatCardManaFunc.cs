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
            return !CheckPhase(CombatCardPhase.InPending) &&
                (!HasConsume || ConsumeStack > 0) &&
                _assignedMana.Count < GetSize() && GetManaCost().TryGetValue(element, out var required) &&
                _assignedMana.Count(e => e.Equals(element)) < required && HasScanOpportunity;
        }

        /// <summary>检查本卡是否已按元素需求满灵，不执行支付或改变状态。</summary>
        public bool HasActivationResources() => _assignedMana.Count == GetSize() &&
            GetManaCost().All(cost => _assignedMana.Count(e => e.Equals(cost.Key)) == cost.Value) && HasScanOpportunity;

        /// <summary>按当前实际蓄灵得出的等待阶段，供核心与主关键词共同读取。</summary>
        public CombatCardPhase ChargePhase => HasActivationResources() ? CombatCardPhase.Fulling : CombatCardPhase.Waiting;

        // 活跃法宝可以边生效边蓄灵；只有等待扫描的卡随灵元变化切换阶段。
        private void RefreshChargePhase()
        {
            if (Phase is CombatCardPhase.Waiting or CombatCardPhase.Fulling or CombatCardPhase.Finished)
                SetPhase(ChargePhase);
        }

        /// <summary>背包转移的接收端；无回调，失败不改变本卡状态或源格。</summary>
        internal bool TryReceiveMana(ElementType element)
        {
            if (!CanAcceptMana(element)) return false;
            _assignedMana.Add(element);
            RefreshChargePhase();
            return true;
        }

        /// <summary>按充入顺序移除匹配的卡上灵元。返回实际剥离数量。</summary>
        internal int StripAssignedMana(int amount, ElementType element)
        {
            int removed = 0;
            for (int i = 0; i < _assignedMana.Count && removed < amount;)
            {
                if (element.Equals(ElementType.None) || _assignedMana[i].Equals(element))
                {
                    _assignedMana.RemoveAt(i);
                    removed++;
                }
                else i++;
            }

            if (removed > 0)
            {
                RefreshChargePhase();
                Scene?.MarkPresentationEffect();
            }
            return removed;
        }

        // 转移方完成提交后再通知，监听者看到的是已释放的源格和已写入的卡。
        internal void DispatchManaCharged(ElementType element)
        {
            var ctx = CreateCtx();
            // 使用事件专用字段，动作包装也会向共享上下文写入 Element。
            ctx.Set("ChargedElement", element.Kind.ToString());
            ctx.Set("LitCount", LitCount);
            ctx.Set("IsFullyCharged", HasActivationResources());
            Scene?.TriggerCombatEvent("OnManaCharged", ctx);
        }

        /// <summary>成功使用或离场后清空蓄灵；重新入场从空状态开始，不再额外支付灵元。</summary>
        internal void ClearAssignedMana()
        {
            _assignedMana.Clear();
            RefreshChargePhase();
        }

        /// <summary>点亮需求由 Size 等量的定义决定，不再读取资源池或旧减费属性。</summary>
        public Dictionary<ElementType, int> GetCombatManaCost() => new(GetManaCost());

        /// <summary>保留旧支付入口，调用时报告当前采用点亮机制。</summary>
        public bool TryPayMana()
        {
            throw new NotSupportedException("灵元进入卡牌即点亮，不存在额外支付。");
        }

        private void DispatchManaUsed(IReadOnlyList<ElementType> paidMana)
        {
            if (paidMana.Count == 0) return;

            // 记录提交前的实付。Lua 监听不依赖表现录制，也不从已清空的卡读账。
            var paid = paidMana.GroupBy(element => element.Kind.ToString())
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var payment = CreateCtx();
            payment.Set("Total", paidMana.Count);
            payment.Set("Kinds", paid.Count);
            foreach (var pair in paid) payment.Set("Paid" + pair.Key, pair.Value);
            Scene.TriggerCombatEvent("OnManaUsed", payment);
        }
    }
}
