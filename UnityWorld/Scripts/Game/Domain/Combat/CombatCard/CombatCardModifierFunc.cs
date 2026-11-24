using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// CombatCard 的 CardModifier 管理逻辑（partial）：
    /// AddCardBuff / ModifierTick / RemoveCardBuff / GetStat 贡献 / 触发器移除
    /// </summary>
    public partial class CombatCard
    {
        /// <summary>当前卡牌持有的状态修正</summary>
        private List<CardModifier> CardModifiers { get; set; } = new();

        /// <summary>卡牌状态移除事件的监听器</summary>
        private DelegateEventListener _cardBuffTriggerListener;
        /// <summary>按移除事件统计的状态引用数</summary>
        private Dictionary<string, int> _cardBuffTriggerRefCounts = new();

        /// <summary>创建供卡牌状态使用的移除事件监听器</summary>
        private void InitCardBuffTriggerListener()
        {
            _cardBuffTriggerListener = new DelegateEventListener(OnCardBuffTriggerEvent);
        }

        /// <summary>为当前卡构造事件作用域键</summary>
        private ScopeKey GetCardScope() => new ScopeKey(Scope.CombatCard, Id.ToString());

        // ══════════════════════════════════════════════════════════
        //  触发器事件响应
        // ══════════════════════════════════════════════════════════

        /// <summary>处理同场移除事件并移除匹配的卡牌状态</summary>
        private void OnCardBuffTriggerEvent(string eventId, ScopeKey scope, object args)
        {
            if (args is APIContext ctx && !ReferenceEquals(ctx.Scene, Scene)) return;
            if (CardModifiers.Count == 0) return;
            using var presentation = Scene?.BeginPresentationEffect(this, "CardBuffTrigger:" + eventId);

            var toRemove = new List<CardModifier>();

            foreach (var mod in CardModifiers)
            {
                if (string.IsNullOrEmpty(mod.RemoveTriggerId)) continue;
                if (mod.RemoveTriggerId != eventId) continue;

                if (mod.ExpirePolicy == ExpirePolicy.TriggerBased)
                {
                    toRemove.Add(mod);
                }
                else
                {
                    var before = mod.CurrentStack;
                    mod.ReduceStack(1);
                    if (mod.CurrentStack != before) Scene?.MarkPresentationEffect();
                    if (mod.IsExpired())
                    {
                        toRemove.Add(mod);
                    }
                }
            }

            foreach (var mod in toRemove)
            {
                UnregisterCardBuffTrigger(mod);
                CardModifiers.Remove(mod);
                Scene?.MarkPresentationEffect();
                Log($"[CardBuff] 触发器移除: {mod.Id} (trigger={eventId})");
            }
            presentation?.Complete();
        }

        // ══════════════════════════════════════════════════════════
        //  触发器注册/注销
        // ══════════════════════════════════════════════════════════

        /// <summary>登记移除事件引用，首次使用时挂载监听器</summary>
        private void RegisterCardBuffTrigger(CardModifier modifier)
        {
            if (string.IsNullOrEmpty(modifier.RemoveTriggerId)) return;

            var triggerId = modifier.RemoveTriggerId;
            if (_cardBuffTriggerListener == null) InitCardBuffTriggerListener();

            if (_cardBuffTriggerRefCounts.TryGetValue(triggerId, out int count))
            {
                _cardBuffTriggerRefCounts[triggerId] = count + 1;
            }
            else
            {
                _cardBuffTriggerRefCounts[triggerId] = 1;
                EventMgr.Instance?.RegisterEvent(
                    $"CombatCard:{Id}:CardBuff",
                    triggerId,
                    GetCardScope(),
                    _cardBuffTriggerListener);
            }
        }

        /// <summary>撤销移除事件引用，计数归零时卸载监听器</summary>
        private void UnregisterCardBuffTrigger(CardModifier modifier)
        {
            if (string.IsNullOrEmpty(modifier.RemoveTriggerId)) return;

            var triggerId = modifier.RemoveTriggerId;
            if (!_cardBuffTriggerRefCounts.TryGetValue(triggerId, out int count)) return;

            count--;
            if (count <= 0)
            {
                _cardBuffTriggerRefCounts.Remove(triggerId);
                EventMgr.Instance?.RemoveEvent(
                    triggerId,
                    GetCardScope(),
                    _cardBuffTriggerListener);
            }
            else
            {
                _cardBuffTriggerRefCounts[triggerId] = count;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  AddCardBuff
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 添加 CardModifier。同 Id 已存在时执行叠层逻辑。
        /// </summary>
        public void AddCardBuff(CardModifier modifier)
        {
            if (modifier == null) return;
            if (modifier.StatModifiers != null)
                foreach (var stat in modifier.StatModifiers) LegacyCombatCompatibility.RequireSupportedStat(stat.StatId);

            // 查重：同 Id 叠层
            var existing = CardModifiers.FirstOrDefault(m => m.Id == modifier.Id);
            if (existing != null)
            {
                var before = (existing.CurrentStack, existing.RemainingTime);
                existing.AddStack(modifier.CurrentStack);
                if (before != (existing.CurrentStack, existing.RemainingTime) && HasPresentationContribution(existing)) Scene?.MarkPresentationEffect();
                Log($"[CardBuff] 叠层: {modifier.Id} (Stack={existing.CurrentStack})");
                return;
            }

            CardModifiers.Add(modifier);
            if (HasPresentationContribution(modifier)) Scene?.MarkPresentationEffect();
            RegisterCardBuffTrigger(modifier);
            Log($"[CardBuff] 添加: {modifier.Id} (Stack={modifier.CurrentStack}, Expire={modifier.ExpirePolicy})");
        }
        /// <summary>判断状态是否有可影响回放的属性贡献</summary>
        private static bool HasPresentationContribution(CardModifier modifier) => modifier.StatModifiers?.Any(s =>
            s.Value != 0 || s.Type is ModifierType.Override or ModifierType.ClampMin or ModifierType.ClampMax) == true;

        // ══════════════════════════════════════════════════════════
        //  RemoveCardBuff
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 主动移除指定 Id 的 CardModifier。未找到时静默跳过。
        /// </summary>
        public void RemoveCardBuff(string id)
        {
            var modifier = CardModifiers.FirstOrDefault(m => m.Id == id);
            if (modifier == null) return;
            UnregisterCardBuffTrigger(modifier);
            CardModifiers.Remove(modifier);
            Scene?.MarkPresentationEffect();
            Log($"[CardBuff] 移除: {id}");
        }

        // ══════════════════════════════════════════════════════════
        //  ModifierTick
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 每战斗 Tick 驱动所有 CardModifier：衰减时间、移除过期。
        /// </summary>
        public void CardModifierTick()
        {
            if (CardModifiers.Count == 0) return;

            foreach (var mod in CardModifiers)
            {
                if (mod.ExpirePolicy == ExpirePolicy.TimeBased || mod.ExpirePolicy == ExpirePolicy.TimeOrStack)
                {
                    if (mod.RemainingTime > 0)
                        mod.RemainingTime--;
                }
            }

            var toRemove = CardModifiers.Where(m => m.IsExpired()).ToList();
            foreach (var mod in toRemove)
            {
                UnregisterCardBuffTrigger(mod);
                CardModifiers.Remove(mod);
                Log($"[CardBuff] 过期移除: {mod.Id}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  Stat 贡献
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 累计所有 CardModifier 对指定属性的贡献，按 ModifierType 分层计算。
        /// 计算顺序：Flat → Percent → ClampMin → ClampMax → Override
        /// </summary>
        public float ApplyCardModifierStat(string statId, float baseValue)
        {
            float flat = 0f;
            float percent = 0f;
            float? clampMax = null;
            float? clampMin = null;
            float? overrideVal = null;

            foreach (var mod in CardModifiers)
            {
                if (mod.StatModifiers == null) continue;
                foreach (var entry in mod.StatModifiers)
                {
                    if (entry.StatId != statId) continue;
                    float val = entry.Value * mod.CurrentStack;
                    switch (entry.Type)
                    {
                        case ModifierType.Flat:
                            flat += val;
                            break;
                        case ModifierType.Percent:
                            percent += val;
                            break;
                        case ModifierType.ClampMax:
                            clampMax = clampMax.HasValue ? Math.Min(clampMax.Value, val) : val;
                            break;
                        case ModifierType.ClampMin:
                            clampMin = clampMin.HasValue ? Math.Max(clampMin.Value, val) : val;
                            break;
                        case ModifierType.Override:
                            overrideVal = val;
                            break;
                    }
                }
            }

            if (overrideVal.HasValue) return overrideVal.Value;

            float result = (baseValue + flat) * (1f + percent);
            if (clampMin.HasValue) result = Math.Max(result, clampMin.Value);
            if (clampMax.HasValue) result = Math.Min(result, clampMax.Value);
            return result;
        }

        /// <summary>
        /// 获取属性最终值（含全场 Modifier OnModifierStat hook 贡献）。
        /// hook 内部读属性应使用 Stats.Get()（裸值）避免递归。
        /// </summary>
        public override float GetStat(string statId)
        {
            float val = base.GetStat(statId);
            // 全场 NpcModifier 的 OnModifierStat hook 贡献
            if (Owner?.Scene != null)
                val += Owner.Scene.CollectModifierStat(this, statId);
            // CardModifier 贡献（含 Flat/Percent/ClampMax/ClampMin/Override）
            val = ApplyCardModifierStat(statId, val);
            return val;
        }

        /// <summary>释放本卡的事件监听。</summary>
        public void Cleanup()
        {
            foreach (var trigger in _cardBuffTriggerRefCounts.Keys)
                EventMgr.Instance?.RemoveEvent(trigger, GetCardScope(), _cardBuffTriggerListener);
            _cardBuffTriggerRefCounts.Clear();
        }
    }
}
