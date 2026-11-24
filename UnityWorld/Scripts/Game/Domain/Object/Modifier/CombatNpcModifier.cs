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
        /// <summary>战斗状态实例编号</summary>
        public string Id { get  ; set  ; }
        /// <summary>战斗状态配置编号</summary>
        public string DefineId { get  ; set  ; }
        /// <summary>状态显示名</summary>
        public string DisplayName { get ; set  ; }
        /// <summary>状态施加来源编号</summary>
        public string SourceId { get  ; set  ; }
        /// <summary>配置的时步持续量</summary>
        public float Duration { get  ; set  ; }
        /// <summary>扣除已过时步的剩余时长</summary>
        public float RemainingTime
        {
            get => Duration - Ticks["Main"];
            set => Ticks["Main"] = Duration - value;
        }
        /// <summary>状态脚本的 Lua 环境</summary>
        public LuaTable env { get  ; set  ; }
        /// <summary>按名称缓存的 Lua 钩子</summary>
        public Dictionary<string, LuaFunction> LuaHooks { get ; set ; } = new();
        /// <summary>当前时步已调用的钩子</summary>
        public HashSet<string> HookUse { get ; set ; } = new();

        /// <summary>状态的持有角色</summary>
        public CombatNpc Owner;    
        /// <summary>状态最后生效的背包轮次</summary>
        internal int LastRound { get; set; } = int.MaxValue;
        /// <summary>属性修正的独立来源键</summary>
        internal string StatSource { get; set; }
        /// <summary>层数是否允许带正负号</summary>
        internal bool SignedStacks { get; private set; }
        // 同一笔结算命中的单次实例一起标记，OnRemove 重入时不能再次兑现。
        /// <summary>该实例是否已被消费</summary>
        internal bool Consumed { get; private set; }

        /// <summary>Lua 在匹配的 Modify 回调里消费自身。实例立即失效，本轮回调结束后执行 OnRemove。</summary>
        public void Consume() => Consumed = true;
        // 蓄力属于当场的 Buff 实例。每次记录实际吸收，同轮达标后只兑现一次。
        /// <summary>护盾蓄积轮次、吸收量和触发状态</summary>
        internal (int Round, long Absorbed, bool Triggered) ShieldChargeProgress { get; set; }

        /// <summary>剩余的持有者扫描轮数。空值表示这个效果没有轮次数上限。</summary>
        public int? RemainingRounds => LastRound == int.MaxValue || Owner == null
            ? null
            : Math.Max(0, LastRound - Owner.Backpack.Round + 1);

        /// <summary>状态附带的属性修正</summary>
        public List<StatModifierEntry> StatModifiers { get ; set ; }
        /// <summary>配置的层数上限</summary>
        public int MaxStack { get  ; set  ; }
        /// <summary>当前状态层数</summary>
        public int CurrentStack { get  ; set  ; }
        /// <summary>叠加时是否重置持续时间</summary>
        public bool RefreshOnStack { get  ; set  ; }
        /// <summary>状态过期方式</summary>
        public ExpirePolicy ExpirePolicy { get  ; set  ; }
        /// <summary>触发移除的事件编号</summary>
        public string RemoveTriggerId { get  ; set  ; }
        /// <summary>按名称累计的时步计数</summary>
        public Dictionary<string, float> Ticks { get  ; set  ; }= new();
        /// <summary>状态持有者所在的战场</summary>
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

        /// <summary>把带符号的层数限制在正负层数上限之间</summary>
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

        /// <summary>保留状态清理入口</summary>
        public void Cleanup()
        {
            
        }

        /// <summary>保留状态的战斗结束入口</summary>
        public void End()
        {
            
        }

        /// <summary>保留状态消息日志入口</summary>
        public void Log(string msg)
        {
            
        }

        /// <summary>保留状态的开战准备入口</summary>
        public void PreStart()
        {
            
        }

        /// <summary>保留状态的开战入口</summary>
        public void Start()
        {
            
        }

        /// <summary>运行状态时步钩子、清空调用记录并推进计时</summary>
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

        /// <summary>以持有者及其战场建立状态回调上下文</summary>
        private APIContext CreateCtx() => new APIContext
        {
            Caster = Owner,
            Scene = Owner?.Scene
        };
    }


}
