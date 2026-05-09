
using NLua;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard : Card,ICombatEntity,ILuaBindable
    {
        public LuaTable env { get; set; }
        public CombatNpc Owner;    
        public CombatCardPhase Phase = CombatCardPhase.WaitResource;
        public Dictionary<string, float> Ticks { get ; set ; } = new();
        

        public void PreStart()
        {
            InitializeLuaCards();
            //执行env的CardData覆写和keyword的应用
            RunKeywordHooks("OnPreStart");
        }

        public void Start()
        {
            Phase = CombatCardPhase.WaitResource;
            RunKeywordHooks("OnStart");
        }

        public void Tick()
        {
            RunKeywordHooks("OnTick");

            // 被动卡：仅调用 OnPassiveTick hook，跳过 CD 循环
            if (Phase == CombatCardPhase.Passive)
            {
                CallLuaHook("OnPassiveTick", new APIContext
                {
                    SourceCard = this,
                    Caster = Owner,
                    Scene = null
                });
                Ticks["Main"]++;
                return;
            }

            if(Phase == CombatCardPhase.Finished) 
            Phase= CombatCardPhase.WaitResource;
            if(Phase == CombatCardPhase.WaitResource) CheckMana();
            if(Phase == CombatCardPhase.InCD) ResetCD();

            //执行env["OnTick"]
            Ticks["Main"]++;
            CDTick();
        }
        public void CDTick()
        {
            var TickSpeed = Stats.Get("CDSpeed");
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
            var ctx = new APIContext
            {
                SourceCard = this,
                Caster = Owner,
                Scene = null
            };
            CallLuaHook("OnContest", ctx);
        }

        public void OnApply()
        {
            //Log($"[{Owner.GetName()}]卡牌生效:[{DisplayName}]");
            var ctx = new APIContext
            {
                SourceCard = this,
                Caster = Owner,
                Scene = null
            };
            CallLuaHook("OnApply", ctx);
            //Trigger:触发结算事件
            Phase = CombatCardPhase.Finished;
        }

        /// <summary>
        /// 通用 Lua Hook 调用：从 env 取函数并以 card:hookName(ctx) 方式调用。
        /// </summary>
        private void CallLuaHook(string hookName, APIContext ctx)
        {
            if (env == null) return;

            var func = env[hookName] as NLua.LuaFunction;
            if (func == null) return;

            try
            {
                func.Call(env, ctx); // card:OnXxx(ctx)
            }
            catch (Exception ex)
            {
                Log($"Lua {hookName} 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置卡牌 Phase（供 Lua 调用）。
        /// 将字符串解析为 CombatCardPhase 枚举，解析失败时输出错误日志且不改变 Phase。
        /// </summary>
        public void SetPhase(string phaseName)
        {
            if (Enum.TryParse<CombatCardPhase>(phaseName, out var phase))
            {
                Phase = phase;
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
            var keywords = BaseData.Keywords;
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
                    func.Call(this, new APIContext
                    {
                        SourceCard = this,
                        Caster = Owner,
                        Scene = null
                    });
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
            combatCard.BaseData = card.BaseData.Clone();
            combatCard.Stats = StatMgr.Instance.CreateBlock(card.Id, typeof(CombatCard));
            combatCard.Ticks.Add("Main",0);
            combatCard.Ticks.Add("CD",0);
            return combatCard;
        }


    }
}