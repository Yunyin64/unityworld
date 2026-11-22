namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard
    {
        private int _currentAmount = -1;

        /// <summary>本场余弹。同一张卡重新入场沿用余弹，无弹药机制时为 -1。</summary>
        public int CurrentAmount
        {
            get => _currentAmount;
            set
            {
                int max = GetAmountMax();
                int next = max < 0 ? -1 : Math.Clamp(value, 0, max);
                int before = _currentAmount;
                if (before == next) return;
                _currentAmount = next;
                Scene?.MarkPresentationEffect();
                if (before > 0 && next == 0)
                    Scene?.TriggerCombatEvent("OnAmmoExhausted", CreateCtx("Amount", before));
            }
        }

        /// <summary>已配置弹药的卡按定义底值和本场属性修正计算容量。</summary>
        public override int GetAmountMax() => IsAmount
            ? (int)Math.Clamp((long)BaseData.AmountMax + (int)GetStat("AmountMaxAdd"), 0, int.MaxValue)
            : -1;

        /// <summary>是否配置了有效的弹药机制。</summary>
        public bool IsAmount => BaseData.AmountMax > 0 && HasKeyword("Amount");

        /// <summary>是否至少还有一发可用弹药。</summary>
        public bool HasAmount => IsAmount && CurrentAmount > 0;

        /// <summary>扣除至多指定发数；正数扣至零时发布一次本卡的弹药耗尽事件。</summary>
        public void ConsumeAmount(int num = 1)
        {
            if (num > 0 && HasAmount) CurrentAmount = Math.Max(0, CurrentAmount - num);
        }

        /// <summary>恢复至多指定发数，返回本次实际补充量。</summary>
        public int RefillAmount(int count)
        {
            if (!IsAmount || count <= 0) return 0;
            int current = Math.Max(0, CurrentAmount);
            int restored = Math.Min(count, Math.Max(0, GetAmountMax() - current));
            if (restored > 0) CurrentAmount = current + restored;
            return restored;
        }

        
        // ── 消耗与耗尽离场 ──

        internal int InitialConsumeStack { get; private set; } = -1;

        /// <summary>当前战斗剩余消耗次数，无消耗关键词时返回 -1。</summary>
        public int ConsumeStack
        {
            get => HasConsume ? GetStack() : -1;
            set
            {
                if (!HasConsume) return;
                var requested = Math.Clamp(value, 0, StackMax);
                if (requested < GetStack()) TryConsume(GetStack() - requested);
                else BaseData.Stack = requested;
            }
        }

        /// <summary>本卡的消耗次数上限。</summary>
        public int StackMax => GetStackMax();

        /// <summary>是否具有消耗次数。</summary>
        public bool HasConsume => HasKeyword("Consume") && StackMax > 0;

        /// <summary>消耗一次并报告是否成功。</summary>
        public bool TryConsume() => TryConsume(1) == 1;

        /// <summary>扣除至多指定份数并广播实际扣量；本次从正数扣到零时再广播耗尽。</summary>
        public int TryConsume(int count)
        {
            if (!HasConsume || count <= 0 || GetStack() <= 0) return 0;
            var consumed = Math.Min(count, GetStack());
            BaseData.Stack -= consumed;
            Scene?.MarkPresentationEffect();
            // 事件记录本次实际扣除，监听者后续再改变存量也不会改写这次事实。
            int remaining = GetStack();
            var scene = Scene;
            if (scene != null)
            {
                var ctx = CreateCtx();
                ctx.Set("Amount", consumed);
                ctx.Set("Stack", remaining);
                scene.TriggerCombatEvent("OnConsumed", ctx);
                if (remaining == 0) scene.TriggerCombatEvent("OnExhausted", ctx);
            }
            return consumed;
        }

        /// <summary>调整消耗次数，最多恢复到配置上限。</summary>
        public void AddConsume(int num = 1)
        {
            if (!HasConsume) return;
            ConsumeStack = (int)Math.Clamp((long)GetStack() + num, 0, StackMax);
        }
    }
}
