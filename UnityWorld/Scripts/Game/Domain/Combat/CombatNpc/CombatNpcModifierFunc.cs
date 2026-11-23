using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// CombatNpc 的 Modifier 管理逻辑（partial）：
    /// AddModifier / ModifierTick / RemoveModifier / EventMgr 触发器集成
    /// </summary>
    public partial class CombatNpc
    {
        private List<CombatNpcModifier> Modifiers { get; set; } = new();
        private int _modifierStatSource;

        /// <summary>
        /// 触发器事件监听者：接收 EventMgr 广播，处理 RemoveTriggerId 匹配的 Modifier。
        /// </summary>
        private DelegateEventListener _modifierTriggerListener;

        /// <summary>当前已注册监听的 triggerId 集合（用于避免重复注册和正确注销）</summary>
        private Dictionary<string, int> _triggerRefCounts = new();

        /// <summary>返回所有未过期的 Modifier</summary>
        public List<CombatNpcModifier> GetAllModifiers()
        {
            return Modifiers.Where(m => !m.IsExpired() && m.LastRound >= Backpack.Round).ToList();
        }

        /// <summary>
        /// 创建当前 CombatNpc 的 Modifier 调用上下文。
        /// </summary>
        private APIContext CreateModifierCtx(CombatNpcModifier mod) => new APIContext
        {
            Caster = this,
            Scene = Scene,
        };

        /// <summary>
        /// 获取当前 CombatNpc 的事件 ScopeKey。
        /// </summary>
        private ScopeKey GetModifierScope() => new ScopeKey(Scope.CombatNpc, Id.ToString());

        /// <summary>
        /// 初始化 Modifier 触发器监听者。应在 CombatNpc 初始化时调用。
        /// </summary>
        private void InitModifierTriggerListener()
        {
            _modifierTriggerListener = new DelegateEventListener(OnModifierTriggerEvent);
        }

        // ══════════════════════════════════════════════════════════
        //  触发器事件响应
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// EventMgr 触发器回调：遍历 Modifiers，处理 RemoveTriggerId 匹配的 Modifier。
        /// </summary>
        private void OnModifierTriggerEvent(string eventId, ScopeKey scope, object args)
        {
            if (args is APIContext ctx && !ReferenceEquals(ctx.Scene, Scene)) return;
            if (Modifiers.Count == 0) return;

            var toRemove = new List<CombatNpcModifier>();
            List<(string Id, long Before, long After)> elementChanges = null;

            foreach (var mod in Modifiers)
            {
                if (string.IsNullOrEmpty(mod.RemoveTriggerId)) continue;
                if (mod.RemoveTriggerId != eventId) continue;

                if (mod.ExpirePolicy == ExpirePolicy.TriggerBased)
                {
                    // TriggerBased：直接标记移除
                    toRemove.Add(mod);
                }
                else
                {
                    // 其他策略：减层，然后检查是否过期
                    bool isElementStatus = IsBaseElementStatus(mod.DefineId);
                    long before = isElementStatus ? ReadElementStacks(mod.DefineId) : 0;
                    // 正负数值层数都向 0 衰减一层。
                    if (mod.SignedStacks)
                        mod.CurrentStack -= Math.Sign(mod.CurrentStack);
                    else
                        mod.ReduceStack();
                    if (isElementStatus)
                    {
                        long after = ReadElementStacks(mod.DefineId);
                        if (before != after) (elementChanges ??= new()).Add((mod.DefineId, before, after));
                    }
                    if (mod.IsExpired())
                    {
                        toRemove.Add(mod);
                    }
                    else RefreshModifierStats(mod);
                }
            }

            foreach (var mod in toRemove)
            {
                DoRemoveModifier(mod);
                Log($"[Modifier] 触发器移除: {mod.DefineId} (trigger={eventId})");
            }

            // 先走完集合变更，监听者可在收到快照后重新添减状态。
            if (elementChanges != null)
                foreach (var (id, before, after) in elementChanges) DispatchElementStackChange(id, before, after);
        }

        // ══════════════════════════════════════════════════════════
        //  AddModifier
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 添加战斗 Modifier。使用正式 ID 和最后生效轮查找可叠层的实例。
        /// </summary>
        /// <param name="defineId">战斗状态定义的 ID。</param>
        /// <param name="stacks">叠加层数（默认 1）。数值状态接受有符号层数。</param>
        /// <param name="duration">持续的战斗 Tick 数。正值启用计时。</param>
        public void AddModifier(string defineId, int stacks = 1,float duration = -1)
            => AddModifierCore(defineId, stacks, duration, null);

        internal void AddRoundModifier(string defineId, int stacks, int rounds)
        {
            if (stacks > 0 && rounds >= 0) AddModifierCore(defineId, stacks, -1, rounds);
        }

        private void AddModifierCore(string defineId, int stacks, float duration, int? rounds)
        {
            var define = CombatNpcModifierDefineMgr.Instance.Get(defineId);
            if (define == null) return;

            bool isElementStatus = IsBaseElementStatus(defineId);
            long before = isElementStatus ? ReadElementStacks(defineId) : 0;
            if (isElementStatus && stacks > 0 && TryConsumeElementImmunity(defineId)) return;
            var lastRound = GetLastEffectRound(Math.Max(0, rounds ?? define.DurationRounds));
            var existing = Modifiers.FirstOrDefault(m => m.DefineId == defineId && m.LastRound == lastRound
                && !m.Consumed && (!m.SignedStacks || !m.IsExpired()));
            if (existing != null)
            {
                if (existing.SignedStacks && stacks == 0) return;
                StackModifier(existing, stacks);
                if (isElementStatus) DispatchElementStackChange(defineId, before);
                return;
            }

            // 首次添加：从 Define 创建实例
            var modifier = CombatNpcModifier.CreateModifier(define);
            modifier.Owner = this;
            modifier.InitializeLuaModifier();
            if (modifier.SignedStacks && stacks == 0) return;
            modifier.CurrentStack = modifier.SignedStacks
                ? modifier.LimitSignedStacks(stacks)
                : isElementStatus && define.MaxStack > 0 ? Math.Min(stacks, define.MaxStack) : stacks;
            modifier.Duration = duration;
            modifier.LastRound = lastRound;
            if (modifier.StatModifiers.Count > 0) modifier.StatSource = $"battle-modifier:{++_modifierStatSource}";
            
            modifier.CallLuaHook<bool>("Apply", modifier.env, CreateModifierCtx(modifier));
            Modifiers.Add(modifier);
            RefreshModifierStats(modifier);
            Scene?.MarkPresentationEffect();

            // 注册触发器事件监听
            RegisterTriggerEvent(modifier);

            Log($"[Modifier] 添加: {defineId} (Stack={modifier.CurrentStack}, Duration={modifier.Duration}, Expire={modifier.ExpirePolicy})");
            if (isElementStatus) DispatchElementStackChange(defineId, before);
        }

        internal static bool IsBaseElementStatus(string defineId) =>
            ElementType.BaseElementBuff.Values.Any(pair => pair.Item1 == defineId || pair.Item2 == defineId);

        private long ReadElementStacks(string defineId) =>
            Modifiers.Where(modifier => modifier.DefineId == defineId).Sum(modifier => Math.Max(0L, modifier.CurrentStack));

        private void DispatchElementStackChange(string defineId, long before, long? committedAfter = null)
        {
            long after = committedAfter ?? ReadElementStacks(defineId);
            if (before == after) return;
            var ctx = new APIContext { Caster = this, Scene = Scene };
            ctx.Set("ChangedBuffId", defineId);
            ctx.Set("Before", before);
            ctx.Set("After", after);
            ctx.Set("Delta", after - before);
            Scene?.TriggerCombatEvent("OnElementStackChanged", ctx);
        }

        /// <summary>减少一个具体五行状态的层数，提交变化后再通知监听者。</summary>
        internal void ReduceElementModifier(CombatNpcModifier modifier)
        {
            long before = ReadElementStacks(modifier.DefineId);
            modifier.ReduceStack();
            if (modifier.IsExpired()) DoRemoveModifier(modifier);
            else RefreshModifierStats(modifier);
            Scene?.MarkPresentationEffect();
            DispatchElementStackChange(modifier.DefineId, before);
        }

        private bool TryConsumeElementImmunity(string defineId)
        {
            var immunity = GetAllModifiers().FirstOrDefault(modifier => modifier.DefineId == $"ElementImmunity_{defineId}");
            if (immunity == null) return false;
            immunity.ReduceStack();
            if (immunity.IsExpired()) DoRemoveModifier(immunity);
            Scene?.MarkPresentationEffect();
            return true;
        }

        /// <summary>按实例的层数规则合并，再统一处理移除、刷新和 Lua 通知。</summary>
        private void StackModifier(CombatNpcModifier modifier, int stacks)
        {
            var before = (modifier.CurrentStack, modifier.RemainingTime);
            if (modifier.SignedStacks)
            {
                modifier.CurrentStack = modifier.LimitSignedStacks((long)modifier.CurrentStack + stacks);
                if (modifier.CurrentStack == 0)
                {
                    DoRemoveModifier(modifier);
                    return;
                }
                if (modifier.RefreshOnStack && modifier.Duration > 0)
                    modifier.RemainingTime = modifier.Duration;
            }
            else
            {
                // 普通状态继续使用原有叠层规则，正向叠加前留足整数空间。
                stacks = Math.Min(stacks, int.MaxValue - Math.Max(0, modifier.CurrentStack));
                modifier.AddStack(stacks);
            }

            if (before != (modifier.CurrentStack, modifier.RemainingTime)) Scene?.MarkPresentationEffect();
            modifier.CallLuaHook<bool>("OnStack", modifier.env, CreateModifierCtx(modifier));
            RefreshModifierStats(modifier);
            Log($"[Modifier] 叠层: {modifier.DefineId} (Stack={modifier.CurrentStack})");
        }

        // ══════════════════════════════════════════════════════════
        //  ModifierTick
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 每战斗 Tick 驱动所有 Modifier：调用 OnTick、衰减时间、移除过期。
        /// </summary>
        public void ModifierTick()
        {
            if (Modifiers.Count == 0) return;
            foreach (var mod in Modifiers.ToArray())
            {
                mod.Tick();
            }
            var toRemove =  Modifiers.Where(c => c.IsExpired()).ToList();
            // 批量移除过期 Modifier
            foreach (var mod in toRemove)
            {
                DoRemoveModifier(mod);
                Log($"[Modifier] 过期移除: {mod.DefineId}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  战斗数值修正管线
        // ══════════════════════════════════════════════════════════

        /// <summary>逐个调用有效 Buff 的 Lua 回调。脚本直接修改本次事件，并可消费自己的实例。</summary>
        public void ModifyHook(string hookName, APIContext ctx)
        {
            if (Modifiers.Count == 0) return;
            var active = GetAllModifiers();
            var hook = "Modify" + hookName;
            foreach (var mod in active)
            {
                if (mod.Consumed) continue;
                ctx.Set("Modifier", mod);
                mod.CallLuaHook<object>(hook, mod.env, ctx);
            }
            // 全部命中的单次实例已失效，OnRemove 触发的新结算不会再次兑现它们。
            foreach (var mod in active)
                if (mod.Consumed) DoRemoveModifier(mod);
        }

        /// <summary>治疗、获盾和直接回蓝共用 Amount 参数，脚本可以读取本次基础点数。</summary>
        private int ModifyAmount(string hook, int amount, APIContext ctx)
        {
            ctx.Set("Amount", amount);
            ModifyHook(hook, ctx);
            return (int)Math.Clamp(ctx.GetValue("Amount", (long)amount), 0, int.MaxValue);
        }

        // ══════════════════════════════════════════════════════════
        //  RemoveModifier
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 主动移除指定定义 ID 的所有实例，并对每个实例调用 Lua OnRemove。
        /// </summary>
        public void RemoveModifier(string defineId)
        {
            foreach (var modifier in Modifiers.Where(m => m.DefineId == defineId).ToArray())
            {
                DoRemoveModifier(modifier);
                Log($"[Modifier] 主动移除: {defineId}");
            }
        }

        private void RefreshModifierStats(CombatNpcModifier modifier)
        {
            if (modifier.StatSource == null) return;
            Stats.RemoveModifiersBySource(modifier.StatSource);
            foreach (var item in modifier.StatModifiers)
                Stats.AddModifier(item.StatId, new StatModifier(item.Type, item.Value * modifier.CurrentStack, modifier.StatSource));
        }

        private long ReadBattleModifierAmount(string id)
            => GetAllModifiers().Where(modifier => modifier.DefineId == id).Sum(modifier => (long)modifier.CurrentStack);

        private long ConsumeBattleModifier(string id)
        {
            var amount = ReadBattleModifierAmount(id);
            if (amount > 0) RemoveModifier(id);
            return amount;
        }

        /// <summary>
        /// 移除状态时执行 Lua OnRemove、注销触发器监听并撤销属性修正。
        /// </summary>
        private void DoRemoveModifier(CombatNpcModifier modifier)
        {
            // 有符号状态先离开集合，避免移除回调重入时消费旧实例或吞掉同 ID 的新状态。
            bool detachFirst = modifier.SignedStacks || modifier.Consumed;
            bool isElementStatus = IsBaseElementStatus(modifier.DefineId);
            long before = isElementStatus ? ReadElementStacks(modifier.DefineId) : 0;
            if (detachFirst && !Modifiers.Remove(modifier)) return;

            modifier.CallLuaHook<bool>("OnRemove", modifier.env, CreateModifierCtx(modifier));
            UnregisterTriggerEvent(modifier);
            if (modifier.StatSource != null) Stats.RemoveModifiersBySource(modifier.StatSource);
            if (!detachFirst) Modifiers.Remove(modifier);
            Scene?.MarkPresentationEffect();
            if (isElementStatus) DispatchElementStackChange(modifier.DefineId, before);
        }

        // ══════════════════════════════════════════════════════════
        //  触发器事件注册/注销
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 为 Modifier 的 RemoveTriggerId 注册 EventMgr 监听。
        /// 使用引用计数避免同一 triggerId 重复注册。
        /// </summary>
        private void RegisterTriggerEvent(CombatNpcModifier modifier)
        {
            if (string.IsNullOrEmpty(modifier.RemoveTriggerId)) return;

            var triggerId = modifier.RemoveTriggerId;
            if (_modifierTriggerListener == null) InitModifierTriggerListener();

            if (_triggerRefCounts.TryGetValue(triggerId, out int count))
            {
                _triggerRefCounts[triggerId] = count + 1;
            }
            else
            {
                _triggerRefCounts[triggerId] = 1;
                EventMgr.Instance?.RegisterEvent(
                    $"CombatNpc:{Id}:Modifier",
                    triggerId,
                    GetModifierScope(),
                    _modifierTriggerListener);
            }
        }

        /// <summary>
        /// 注销 Modifier 的 RemoveTriggerId 监听。
        /// 引用计数归零时才真正从 EventMgr 注销。
        /// </summary>
        private void UnregisterTriggerEvent(CombatNpcModifier modifier)
        {
            if (string.IsNullOrEmpty(modifier.RemoveTriggerId)) return;

            var triggerId = modifier.RemoveTriggerId;
            if (!_triggerRefCounts.TryGetValue(triggerId, out int count)) return;

            count--;
            if (count <= 0)
            {
                _triggerRefCounts.Remove(triggerId);
                EventMgr.Instance?.RemoveEvent(
                    triggerId,
                    GetModifierScope(),
                    _modifierTriggerListener);
            }
            else
            {
                _triggerRefCounts[triggerId] = count;
            }
        }
    }
}
