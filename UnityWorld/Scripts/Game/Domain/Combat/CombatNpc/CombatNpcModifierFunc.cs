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

        /// <summary>
        /// 触发器事件监听者：接收 EventMgr 广播，处理 RemoveTriggerId 匹配的 Modifier。
        /// </summary>
        private DelegateEventListener _modifierTriggerListener;

        /// <summary>当前已注册监听的 triggerId 集合（用于避免重复注册和正确注销）</summary>
        private Dictionary<string, int> _triggerRefCounts = new();

        /// <summary>返回所有未过期的 Modifier</summary>
        public List<CombatNpcModifier> GetAllModifiers()
        {
            return Modifiers.Where(m => !m.IsExpired()).ToList();
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
            if (Modifiers.Count == 0) return;

            var toRemove = new List<CombatNpcModifier>();

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
                    mod.ReduceStack(1);
                    if (mod.IsExpired())
                    {
                        toRemove.Add(mod);
                    }
                }
            }

            foreach (var mod in toRemove)
            {
                DoRemoveModifier(mod);
                Log($"[Modifier] 触发器移除: {mod.DefineId} (trigger={eventId})");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  AddModifier
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 添加战斗 Modifier。同 DefineId 已存在时执行叠层逻辑。
        /// </summary>
        /// <param name="defineId">CombatNpcModifierDefine ID</param>
        /// <param name="stacks">叠加层数（默认 1）</param>
        public void AddModifier(string defineId, int stacks = 1)
        {
            // 查重：是否已有同 DefineId 的 Modifier
            var existing = Modifiers.FirstOrDefault(m => m.DefineId == defineId);
            if (existing != null)
            {
                // 叠层逻辑
                StackModifier(existing, stacks);
                return;
            }

            // 首次添加：从 Define 创建实例
            var define = CombatNpcModifierDefineMgr.Instance.Get(defineId);
            if (define == null) return;

            var modifier = CombatNpcModifier.CreateModifier(define);
            modifier.Owner = this;
            modifier.CallLuaHook<bool>("OnApply", modifier.env, CreateModifierCtx(modifier));
            Modifiers.Add(modifier);

            // 注册触发器事件监听
            RegisterTriggerEvent(modifier);

            Log($"[Modifier] 添加: {defineId} (Stack={modifier.CurrentStack}, Duration={modifier.Duration}, Expire={modifier.ExpirePolicy})");
        }

        /// <summary>
        /// 叠层逻辑：使用统一的 AddStack 扩展方法，含 MaxStack 限制和 RefreshOnStack。
        /// </summary>
        private void StackModifier(CombatNpcModifier modifier, int stacks)
        {
            modifier.AddStack(stacks);

            // 调用 OnStack hook
            modifier.CallLuaHook<bool>("OnStack", modifier.env, CreateModifierCtx(modifier));

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
            foreach (var mod in Modifiers)
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
        //  拼点修正管线
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 拼点修正管线：构建 ContestData 后、入槽前，遍历 Modifier 调用 ModifyContest hook。
        /// hook 签名：ModifyContest(env, ctx)，ctx.ContestData 携带拼点数据。
        /// </summary>
        public void ModifyContest(ContestData contestData)
        {
            if (Modifiers.Count == 0) return;

            foreach (var mod in Modifiers)
            {
                var ctx = CreateModifierCtx(mod);
                mod.CallLuaHook<bool>("ModifyContest", mod.env, ctx, contestData);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  RemoveModifier
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 主动移除指定 DefineId 的 Modifier，调用 OnRemove hook。未找到时静默跳过。
        /// </summary>
        public void RemoveModifier(string defineId)
        {
            var modifier = Modifiers.FirstOrDefault(m => m.DefineId == defineId);
            if (modifier == null) return;

            DoRemoveModifier(modifier);
            Log($"[Modifier] 主动移除: {defineId}");
        }

        /// <summary>
        /// 内部统一移除逻辑：调用 OnRemove hook → 注销触发器监听 → 从列表移除。
        /// </summary>
        private void DoRemoveModifier(CombatNpcModifier modifier)
        {
            modifier.CallLuaHook<bool>("OnRemove", modifier.env, CreateModifierCtx(modifier));
            UnregisterTriggerEvent(modifier);
            Modifiers.Remove(modifier);
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
