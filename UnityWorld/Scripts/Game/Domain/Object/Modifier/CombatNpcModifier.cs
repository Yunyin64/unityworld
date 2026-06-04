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
    public class CombatNpcModifier : IModifierBase,IFormDefine<CombatNpcModifierDefine>,ILuaBindable,ICombatEntity
    {
        public string Id { get  ; set  ; }
        public string DefineId { get  ; set  ; }
        public string DisplayName { get ; set  ; }
        public string SourceId { get  ; set  ; }
        public float Duration { get  ; set  ; }
        public float RemainingTime
        {
            get => Duration - Ticks["Main"];
            set => Ticks["Main"] = Duration - value;
        }
        public LuaTable env { get  ; set  ; }
        public Dictionary<string, LuaFunction> LuaHooks { get ; set ; } = new();
        public HashSet<string> HookUse { get ; set ; } = new();

        public CombatNpc Owner;    

        public List<StatModifierEntry> StatModifiers { get ; set ; }
        public int MaxStack { get  ; set  ; }
        public int CurrentStack { get  ; set  ; }
        public bool RefreshOnStack { get  ; set  ; }
        public ExpirePolicy ExpirePolicy { get  ; set  ; }
        public string RemoveTriggerId { get  ; set  ; }
        public Dictionary<string, float> Ticks { get  ; set  ; }= new();
        public CombatScene Scene {get => Owner.Scene;set => Owner.Scene = value;}

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
                MaxStack = source.MaxStack,
                CurrentStack = 1,
                RefreshOnStack = source.RefreshOnStack,
                StatModifiers = source.StatModifiers,
                ExpirePolicy = source.ExpirePolicy,
                RemoveTriggerId = source.RemoveTriggerId,
            };
            buff.Ticks.Add("Main",0);

            // 加载 Lua env 并预扫描 hooks
            buff.InitializeLuaModifier();

            return buff;
        }

        /// <summary>
        /// 加载 Lua 脚本并注入 C# 引用（与 CombatCard.InitializeLuaCards 对齐）。
        /// </summary>
        public void InitializeLuaModifier()
        {
            var luaMgr = LuaMgr.Instance;
            if (luaMgr == null) return;

            env = luaMgr.LoadModifierScript(DefineId);
            if (env != null)
            {
                env["m_Self"] = this;
                env["m_Owner"] = Owner;
            }
            LuaHooks = LuaMgr.ScanLuaHooks(env);
        }

        public void Cleanup()
        {
            
        }

        public void End()
        {
            
        }

        public void Log(string msg)
        {
            
        }

        public void PreStart()
        {
            
        }

        public void Start()
        {
            
        }

        public void Tick()
        {
            this.CallLuaHook<bool>("OnTick", env, CreateCtx());
            HookUse.Clear();
            if (Duration > 0)
            {
                Ticks["Main"] += 1;
            }
        }
        
        /// <summary>
        /// 统一 Lua hook 调用入口（与 CombatCard.CallLua 对齐）。
        /// self = env（含 m_Self/m_Owner），ctx 为调用上下文。
        /// </summary>
        public void CallLua(string hookName, APIContext ctx = null)
        {
            if (ctx == null) ctx = CreateCtx();
            this.CallLuaHook<bool>(hookName, env, ctx);
        }

        private APIContext CreateCtx() => new APIContext
        {
            Caster = Owner,
            Scene = Owner?.Scene
        };
    }


}
