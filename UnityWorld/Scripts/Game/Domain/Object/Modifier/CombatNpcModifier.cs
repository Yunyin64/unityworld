using System.Collections.Generic;
using UnityWorld.Game.Data;
using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;
using NLua;
namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 战斗修正源：战斗场景中 CombatNpc 的临时修正，按战斗 Tick 衰减。
    /// </summary>
    public class CombatNpcModifier : IModifierBase,IFormDefine<CombatNpcModifierDefine>,ILuaBindable
    {
        public string Id { get  ; set  ; }
        public string DefineId { get  ; set  ; }
        public string DisplayName { get ; set  ; }
        public string SourceId { get  ; set  ; }
        public float Duration { get  ; set  ; }
        public float RemainingTime { get  ; set  ; }
        public LuaTable env { get  ; set  ; }

        public List<StatModifierEntry> StatModifiers { get ; set ; }
        public int MaxStack { get  ; set  ; }
        public int CurrentStack { get  ; set  ; }
        public bool RefreshOnStack { get  ; set  ; }

        // ── Lua Hook 调用 ──────────────────────────────────────────

        /// <summary>
        /// 调用 env 中的 Lua Hook 函数。
        /// env 为 null 或 hook 不存在时静默跳过，异常时输出错误日志不中断。
        /// 支持传入额外参数（如 DamageInfo），Lua 签名：hookName(env, npc, ...)
        /// </summary>
        public void CallLuaHook(string hookName, CombatNpc npc, params object[] extraArgs)
        {
            if (env == null) return;

            var func = env[hookName] as LuaFunction;
            if (func == null) return;

            try
            {
                // 同步运行时状态到 Lua env
                env["CurrentStack"] = CurrentStack;

                // 构建参数列表：env, npc, ...extraArgs
                var args = new object[2 + extraArgs.Length];
                args[0] = env;
                args[1] = npc;
                for (int i = 0; i < extraArgs.Length; i++)
                    args[2 + i] = extraArgs[i];

                func.Call(args);
            }
            catch (System.Exception ex)
            {
                LogMgr.Err("[CombatNpcModifier] '{0}' hook '{1}' 异常: {2}", DefineId, hookName, ex.Message);
            }
        }

        // ── 工厂方法 ──────────────────────────────────────────

        /// <summary>
        /// 从本定义实例化一个 <see cref="CombatNpcModifier"/>。
        /// </summary>
        public static CombatNpcModifier CreateModifier(CombatNpcModifierDefine source)
        {
            var buff =  new CombatNpcModifier()
            {
                DefineId = source.ID,
                DisplayName = source.DisplayName,
                Duration = source.Duration,
                RemainingTime = source.Duration,
                MaxStack = source.MaxStack,
                CurrentStack = 1,
                RefreshOnStack = source.RefreshOnStack,
                StatModifiers = source.StatModifiers,
            };

            // 加载 Lua env（按约定路径，不存在则为 null）
            var luaMgr = LuaMgr.Instance;
            if (luaMgr != null)
            {
                buff.env = luaMgr.LoadModifierScript(source.ID);
            }

            return buff;
        }
            
    }


}
