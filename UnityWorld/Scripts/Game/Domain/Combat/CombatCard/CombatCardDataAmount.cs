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
        
    }
}