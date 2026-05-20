using NLua;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard 
    {

        ///弹药机制.一般用在法宝、次数制的卡牌上。归0会无法结算CD，但任然可以触发
        public int CurrentAmount {get;set;} = -1;
        private int AmountMax { get; set; } = -1;
        public int GetAmountMax()
        {
            if(IsAmount)  return AmountMax + (int)GetStat("AmountMaxAdd");
            return -1;
        }
        /// <summary>
        /// 是否有弹药机制
        /// </summary>
        public  bool IsAmount => CurrentAmount >= 0 && AmountMax > 0; 

        /// <summary>
        /// 是否有弹药空余
        /// </summary>
        public  bool HasAmount => IsAmount && CurrentAmount > 0;

        public void ConsumeAmount(int num = 1)
        {
            CurrentAmount = Math.Max(0, CurrentAmount - num);
            //Trigger:触发消耗弹药事件
            
            //Trigger:如果弹药耗尽，触发耗尽弹药事件
            if(CurrentAmount == 0){}
        }

        ///消耗机制。一般用在丹药、消耗制的卡牌上。归0会直接移除

        public int ConsumeStack { get; set; } = -1;
        //上限先默认9
        public int StackMax => 9;

        public bool HasConsume => ConsumeStack >= 0;

        public bool TryConsume()
        {
            if(ConsumeStack <= 0) return false;
            ConsumeStack--;
            //Trigger:触发单次消耗事件
            if(ConsumeStack == 0)
            {
                //触发耗尽逻辑
            }
            return true;
        }
        public int TryConsume(int count)
        {
            int lastRemoved = 0;
            for (int i = 0; i < count; i++)
            {
                if (TryConsume()) lastRemoved = i + 1;
                else break;
            }
            //Trigger:触发总消耗事件
            return lastRemoved;
        }

        public void AddConsume(int num = 1)
        {
            ConsumeStack += num;
            ConsumeStack = Math.Clamp(ConsumeStack,0,StackMax);
        }

        ///灵元消耗机制
        private Dictionary<ElementType, int> CombatManaCost = new();
        public Dictionary<ElementType, int> GetCombatManaCost()
        {
            CombatManaCost.Clear();
            foreach (var kv in GetManaCost())
            {
                var final = Math.Max(0, kv.Value + (int)GetStat($"ManaAdj_{kv.Key}"));
                if (final > 0) CombatManaCost[kv.Key] = final;
            }
            return CombatManaCost;
        }
        public int GetDeckIndex() => Owner?.GetCardDeck().IndexOf(this) ?? -1;
        public float GetCDMax()
        {
            return GetCooldown() + GetStat("CDTickAdj");
        }

        
    }
}