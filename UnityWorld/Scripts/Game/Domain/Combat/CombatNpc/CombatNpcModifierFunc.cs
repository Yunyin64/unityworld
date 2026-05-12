using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// CombatNpc 的 Modifier 管理逻辑（partial）：
    /// AddModifier / ModifierTick / RemoveModifier
    /// </summary>
    public partial class CombatNpc
    {
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
            if (define == null)
            {
                Log($"[Modifier] Define 不存在: {defineId}");
                return;
            }

            var modifier = CombatNpcModifier.CreateModifier(define);

            // 调用 OnApply hook
            modifier.CallLuaHook("OnApply", this);

            Modifiers.Add(modifier);
            Log($"[Modifier] 添加: {defineId} (Stack={modifier.CurrentStack}, Duration={modifier.Duration})");
        }

        /// <summary>
        /// 叠层逻辑：累加 CurrentStack，受 MaxStack 限制；RefreshOnStack 时重置时间。
        /// </summary>
        private void StackModifier(CombatNpcModifier modifier, int stacks)
        {
            int newStack = modifier.CurrentStack + stacks;

            // MaxStack 限制（0 = 无上限）
            if (modifier.MaxStack > 0 && newStack > modifier.MaxStack)
            {
                newStack = modifier.MaxStack;
            }

            modifier.CurrentStack = newStack;

            // RefreshOnStack：重置 RemainingTime
            if (modifier.RefreshOnStack && modifier.Duration > 0)
            {
                modifier.RemainingTime = modifier.Duration;
            }

            // 调用 OnStack hook
            modifier.CallLuaHook("OnStack", this);

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

            var toRemove = new List<CombatNpcModifier>();

            foreach (var mod in Modifiers)
            {
                // 调用 OnTick hook
                mod.CallLuaHook("OnTick", this);

                // 衰减有限时 Modifier
                if (mod.Duration > 0)
                {
                    mod.RemainingTime -= 1;
                    if (mod.RemainingTime <= 0)
                    {
                        toRemove.Add(mod);
                    }
                }
            }

            // 批量移除过期 Modifier
            foreach (var mod in toRemove)
            {
                mod.CallLuaHook("OnRemove", this);
                Modifiers.Remove(mod);
                Log($"[Modifier] 过期移除: {mod.DefineId}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  拼点修正管线
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 拼点修正管线：构建 ContestData 后、入槽前，遍历 Modifier 调用 ModifyContest hook。
        /// hook 签名：ModifyContest(env, npc, contestData)
        /// </summary>
        public void ModifyContest(ContestData contestData)
        {
            if (Modifiers.Count == 0) return;

            foreach (var mod in Modifiers)
            {
                mod.CallLuaHook("ModifyContest", this, contestData);
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

            modifier.CallLuaHook("OnRemove", this);
            Modifiers.Remove(modifier);
            Log($"[Modifier] 主动移除: {defineId}");
        }
    }
}
