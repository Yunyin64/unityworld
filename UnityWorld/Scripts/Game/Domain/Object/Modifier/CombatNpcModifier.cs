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
        internal int LastRound { get; set; } = int.MaxValue;
        internal string StatSource { get; set; }
        internal bool SignedStacks { get; private set; }
        // 同一笔结算命中的单次实例一起标记，OnRemove 重入时不能再次兑现。
        internal bool Consumed { get; private set; }

        /// <summary>Lua 在匹配的 Modify 回调里消费自身。实例立即失效，本轮回调结束后执行 OnRemove。</summary>
        public void Consume() => Consumed = true;
        // 蓄力属于当场的 Buff 实例。每次记录实际吸收，同轮达标后只兑现一次。
        internal (int Round, long Absorbed, bool Triggered) ShieldChargeProgress { get; set; }

        /// <summary>剩余的持有者扫描轮数。空值表示这个效果没有轮次数上限。</summary>
        public int? RemainingRounds => LastRound == int.MaxValue || Owner == null
            ? null
            : Math.Max(0, LastRound - Owner.Backpack.Round + 1);

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

            return buff;
        }

        /// <summary>数值状态在层数归零或计时结束时过期，轮数由持有者另行判断。</summary>
        internal bool IsExpired() => Consumed || (SignedStacks
            ? CurrentStack == 0 || (Duration > 0 && RemainingTime <= 0)
            : IModifierBaseExt.IsExpired(this));

        internal int LimitSignedStacks(long stacks)
        {
            long limit = MaxStack > 0 ? MaxStack : int.MaxValue;
            return (int)Math.Clamp(stacks, -limit, limit);
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
                // 层数规则随同名脚本声明；普通状态继续使用原有过期策略。
                SignedStacks = env["SignedStacks"] is true;
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
