
using NLua;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard : Card,ICombatEntity,ILuaBindable
    {
        public LuaTable env { get; set; }
        public Dictionary<string, LuaFunction> LuaHooks { get; set; } = new();
        public CombatNpc Owner;    
        private CombatCardPhase Phase = CombatCardPhase.WaitResource;
        public Dictionary<string, float> Ticks { get ; set ; } = new();
        public CombatScene Scene { get ; set ; }
        

        public void PreStart()
        {
            InitializeLuaCards();
            //执行env的CardData覆写和keyword的应用
            RunKeywordHooks("OnPreStart");
        }

        public void Start()
        {
            SetPhase(CombatCardPhase.WaitResource);
            RunKeywordHooks("OnStart");
        }

        public void Tick()
        {
            RunKeywordHooks("OnTick");
            if (Phase == CombatCardPhase.Passive){}
            if(Phase == CombatCardPhase.Finished)  SetPhase(CombatCardPhase.WaitResource);
            if(Phase == CombatCardPhase.WaitResource) CheckMana();

            if(Phase == CombatCardPhase.InCD)
            {
                CDTick();
                ResetCD();
            }
            this.CallLuaHook("OnTick", env, CreateCtx());
            Ticks["Main"]++;
        }
        public void CDTick()
        {
            var TickSpeed = GetStat("CDSpeed");
            var CDadd = 1f;
            if(TickSpeed > 0) CDadd = CDadd*(1+TickSpeed/10);
            if(TickSpeed < 0)
            {
                CDadd = CDadd / (1 + (-TickSpeed) / 10f);
            }
            if(CDadd <= 0.1f) CDadd = 0;
            Ticks["CD"] += CDadd;
        }


        public void OnUse()
        {
            //Log($"[{Owner.GetName()}]使用卡牌:[{DisplayName}]");
            //Trigger:触发使用事件
            OnContest();
            if(Phase == CombatCardPhase.Ready){
                OnApply();
            }
        }

        public void OnContest()
        {
            this.CallLuaHook("OnContest", env, CreateCtx());
        }

        public void OnApply()
        {
            //Log($"[{Owner.GetName()}]卡牌生效:[{DisplayName}]");
            this.CallLuaHook("OnApply", env, CreateCtx());

            // 广播卡牌使用事件，供 Modifier 触发器响应
            EventMgr.Instance?.TriggerEvent("OnApply", this,
                (Scope.CombatNpc, Owner?.Id.ToString() ?? ""),
                (Scope.CombatCard, Id.ToString()));

            SetPhase(CombatCardPhase.Finished);
        }

        /// <summary>
        /// 创建当前卡牌的默认 APIContext。
        /// </summary>
        private APIContext CreateCtx() => new APIContext
        {
            SourceCard = this,
            Caster = Owner,
            Scene = Owner?.Scene
        };
        
        
        public CombatCardPhase GetPhase() => Phase;
        public void SetPhase(CombatCardPhase phase) => Phase = phase;

        /// <summary>
        /// 设置卡牌 Phase（供 Lua 调用）。
        /// 将字符串解析为 CombatCardPhase 枚举，解析失败时输出错误日志且不改变 Phase。
        /// </summary>
        public void SetPhase(string phaseName)
        {
            if (Enum.TryParse<CombatCardPhase>(phaseName, out var phase))
            {
                SetPhase(phase);
            }
            else
            {
                Log($"SetPhase 失败：无法解析 '{phaseName}' 为 CombatCardPhase");
            }
        }

        /// <summary>
        /// 遍历卡牌 Keywords 列表，查 LuaMgr 注册表调用对应 hook 函数。
        /// keyword 未注册 → 报错日志；keyword 存在但无对应 hook → 静默跳过。
        /// </summary>
        private void RunKeywordHooks(string hookName)
        {
            var keywords = GetKeywords();
            if (keywords == null || keywords.Count == 0) return;

            var luaMgr = LuaMgr.Instance;
            if (luaMgr == null) return;

            foreach (var kw in keywords)
            {
                var kwTable = luaMgr.GetKeyword(kw);
                if (kwTable == null)
                {
                    Log($"Keyword '{kw}' 未注册");
                    continue;
                }

                var func = kwTable[hookName] as NLua.LuaFunction;
                if (func == null) continue; // hook 不存在，静默跳过

                try
                {
                    func.Call(this, CreateCtx());
                }
                catch (Exception ex)
                {
                    Log($"Keyword '{kw}' hook '{hookName}' 异常: {ex.Message}");
                }
            }
        }


        public void End()
        {
            RunKeywordHooks("OnEnd");
        }
        public void Cleanup()
        {
             
        }
        public void Log(string msg)
        {
            CombatScene.Log($"[Card|{DisplayName}] {msg}");
        }


        public static CombatCard CreateFromData(Card card)
        {
            var combatCard = new CombatCard();
            combatCard.Id = card.Id;
            combatCard.DefineId = card.DefineId;
            combatCard.DisplayName = card.DisplayName;
            combatCard.Stats = StatMgr.Instance.CreateBlock(card.Id, typeof(CombatCard));
            combatCard.Ticks.Add("Main",0);
            combatCard.Ticks.Add("CD",0);
            return combatCard;
        }

        /// <summary>
        /// 获取属性最终值（含全场 Modifier OnModifierStat hook 贡献）。
        /// hook 内部读属性应使用 Stats.Get()（裸值）避免递归。
        /// </summary>
        public override float GetStat(string statId)
        {
            float val = base.GetStat(statId);
            if (Owner?.Scene != null)
                val += Owner.Scene.CollectModifierStat(this, statId);
            return val;
        }


    }
}