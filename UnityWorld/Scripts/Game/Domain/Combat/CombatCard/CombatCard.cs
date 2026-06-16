
using NLua;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard : Card,ICombatEntity,ILuaBindable
    {
        public LuaTable env { get; set; }
        public Dictionary<string, LuaFunction> LuaHooks { get; set; } = new();
        public HashSet<string> HookUse { get; set; } = new();
        public CombatNpc Owner;    
        private CombatCardPhase Phase = CombatCardPhase.Waiting;

        private bool isReady = false;
        public void SetReady(bool ready) => isReady = ready;
        public bool IsReady() => isReady;
        public Dictionary<string, float> Ticks { get ; set ; } = new();
        public CombatScene Scene
        {
            get => Owner?.Scene;
            set => Owner.Scene = value;
        }

        public void PreStart()
        {
            InitializeLuaCards();
            CallLua("PreStart");
        }

        public void Start()
        {
            Ticks["Main"] = 0;
            Ticks["CD"] = 0;
            SetPhase(CombatCardPhase.Waiting);
            CallLua("Start");
        }
        public void ReLoad()
        {
            if(CheckPhase( CombatCardPhase.Finished)) SetPhase(CombatCardPhase.Waiting);
        }

        public void Tick()
        {
            ReLoad();
            CallLua("Tick");
            Scene.TriggerCombatEvent("OnTick", CreateCtx());

            if(CheckPhase(CombatCardPhase.InCD)) CDTick();
            CardModifierTick();
            HookUse.Clear();
            Ticks["Main"]++;
            

        }
        public void CDTick()
        {
            var CDadd = GetCDAdd();
            Ticks["CD"] += CDadd;
            if (Ticks["CD"] >= GetCDMax())
            {
                SetPhase(CombatCardPhase.CDFull);
            }
        }

        private float GetCDAdd()
        {
            var TickSpeed = GetStat("CDSpeed");
            var CDadd = 1f;
            if(TickSpeed > 0) CDadd = CDadd*(1+TickSpeed/10);
            if(TickSpeed < 0)
            {
                CDadd = CDadd / (1 + (-TickSpeed) / 10f);
            }
            if(CDadd <= 0.1f) CDadd = 0;
            return CDadd;
        }

        /// <summary>按当前CDSpeed计算还需多少真实Tick才能CD满，返回-1表示无法完成</summary>
        public int GetCDTickRemaining()
        {
            var CDadd = GetCDAdd();
            if(CDadd <= 0.1f) return -1;
            var remaining = GetCDMax() - Ticks["CD"];
            if(remaining <= 0) return 0;
            return (int)Math.Ceiling(remaining / CDadd);
        }



        public void Use()
        {
            //Log($"[{Owner.GetName()}]使用卡牌:[{DisplayName}]");
            //Trigger:触发使用事件
            Contest();
            if(IsReady() && !CheckPhase(CombatCardPhase.InPending)) Apply();
            Scene.TriggerCombatEvent("OnUse", CreateCtx());
        }

        public void Contest()
        {
            CallLua("Contest");
        }

        public void Apply()
        {
            CallLua("Apply");
            Scene.TriggerCombatEvent("OnApply", CreateCtx());
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
        private APIContext CreateCtx<T>(string key,T value)
        {
            var api = CreateCtx();
            api.Set<T>(key,value);
            return api;
        }
        
        public CombatCardPhase GetPhase(CombatCardPhase phase) => Phase;
        
        public bool CheckPhase(CombatCardPhase phase) => Phase == phase;
        public void SetPhase(CombatCardPhase phase) => Phase = phase;

        /// <summary>
        /// 设置卡牌 Phase（供 Lua 调用）。
        /// 将字符串解析为 CombatCardPhase 枚举，解析失败时输出错误日志且不改变 Phase。
        /// </summary>
        public void SetPhase(string phaseName)
        {
            if (Enum.TryParse<CombatCardPhase>(phaseName, out var phase)) SetPhase(phase);
            else Log($"SetPhase 失败：无法解析 '{phaseName}' 为 CombatCardPhase");
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

        public void CallLua(string hookName,APIContext eventCtx = null)
        {
            if(eventCtx == null) eventCtx = CreateCtx();
            this.CallLuaHook<bool>(hookName, env, eventCtx);
            RunKeywordHooks(hookName);
        }

        public void End()
        {
            
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
            combatCard.ParentCardId = card.ParentCardId;
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
            // 全场 NpcModifier 的 OnModifierStat hook 贡献
            if (Owner?.Scene != null)
                val += Owner.Scene.CollectModifierStat(this, statId);
            // CardModifier 贡献（含 Flat/Percent/ClampMax/ClampMin/Override）
            val = ApplyCardModifierStat(statId, val);
            return val;
        }


    }
}