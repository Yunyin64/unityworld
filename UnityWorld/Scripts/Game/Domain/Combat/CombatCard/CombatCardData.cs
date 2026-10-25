using NLua;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard 
    {
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
        /// <summary>点亮需求由 Size 等量的定义决定，不再读取资源池或旧减费属性。</summary>
        public Dictionary<ElementType, int> GetCombatManaCost() => new(GetManaCost());

        public int GetDeckIndex() => Owner?.GetField().IndexOf(this) ?? -1;
        public float GetCDMax()
        {
            return GetCooldown() + GetStat("CDTickAdj");
        }


        
    }
}