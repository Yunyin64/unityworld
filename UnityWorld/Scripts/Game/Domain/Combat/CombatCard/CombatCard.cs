
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
            
        }

        public void Start()
        {
            Phase = CombatCardPhase.WaitResource;

            //处理card的被动、关键词这些逻辑
        }

        public void Tick()
        {
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
            CombatScene.Log($"[CombatCard] 使用卡牌: {DisplayName}");
            //Trigger:触发使用事件
            OnContest();
            if(Phase == CombatCardPhase.Ready){
                OnApply();
            }
        }
        public void OnContest()
        {
            //执行env["OnContest"]
        }
        public void OnApply()
        {
            CombatScene.Log($"[CombatCard] 卡牌生效: {DisplayName}");
            //执行env["OnApply"]
            //Trigger:触发结算事件
            Phase= CombatCardPhase.Finished;
        }


        public void End()
        {
             
        }
        public void Cleanup()
        {
             
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