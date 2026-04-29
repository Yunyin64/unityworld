using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        public bool TryCostMana(Dictionary<ElementType, int> cost)
        {
            if (CanAffordMana(cost))
            {
                ConsumeMana(cost);
            }
            return true;
        }
        public void DoManaDraw()
        {
            if (Mp <= 0f) return;
            int ManaConvertCD = (int)(Stats.Get("ManaConvertCD")*10);
            if(Ticks["ManaConvert"] >= ManaConvertCD || Ticks["Main"] == 0)
            {
                Ticks["ManaConvert"] = 0;
                DrawMana((int)Stats.Get("ManaConvertCost"));
            } 
        }

        public void DrawMana(int costvalue)
        {
                int actualCost = (int)Math.Min(Mp, costvalue);
                Mp -= actualCost;

                // 按五行亲和权重随机产出灵元
                var affinity = GetAffinity();
                var weights = new (ElementType Key, int Weight)[]
                {
                    (ElementType.Jin, affinity.Jin),
                    (ElementType.Mu, affinity.Mu),
                    (ElementType.Shui , affinity.Shui),
                    (ElementType.Huo , affinity.Huo),
                    (ElementType.Tu , affinity.Tu)
                };
                int totalWeight = weights.Sum(w => w.Weight);

                for (int i = 0; i < weights.Length; i++)
                    {
                        int roll = Soul.Random(0, totalWeight);
                        int cumulative = 0;
                        var chosen = weights[0].Key;
                        foreach (var w in weights)
                        {
                            cumulative += w.Weight;
                            if (roll < cumulative) { chosen = w.Key; break; }
                        }

                        if (ManaPool.ContainsKey(chosen))
                            ManaPool[chosen]++;
                        else
                            ManaPool[chosen] = 1;
                    }
               
        }
        public void ManaConvert(Dictionary<ElementType, int> cost)
        {
            var totalConsumed = 0;
            if (CanAffordMana(cost))
            {
                totalConsumed = ConsumeMana(cost);
            }
            RecoverMana(totalConsumed);
        }
        
        public int GetManaCount(ElementType element)
        {
            return ManaPool.TryGetValue(element, out var v) ? v : 0;
        }

        /// <summary>
        /// 检查 NPC 的灵元池是否满足指定消耗。
        /// </summary>
        public bool CanAffordMana(Dictionary<ElementType, int> manaCost)
        {
            if (manaCost == null || manaCost.Count == 0) return true;

            foreach (var (key, required) in manaCost)
            {
                int available = ManaPool.TryGetValue(key, out var v) ? v : 0;
                if (available < required) return false;
            }
            return true;
        }

        /// <summary>
        /// 从 NPC 灵元池扣除消耗。
        /// 调用前应先确认 CanAffordMana 返回 true。
        /// </summary>
        public int ConsumeMana(Dictionary<ElementType, int> manaCost)
        {
            int totalConsumed = 0;  
            if (manaCost == null || manaCost.Count == 0) return 0;

            foreach (var (key, amount) in manaCost)
            {
                if (ManaPool.ContainsKey(key))
                {
                    //Trigger:触发消耗某类灵元事件
                    totalConsumed += amount;
                    ManaPool[key] -= amount;
                    if (ManaPool[key] <= 0) LogMgr.Warn($"[CombatNpc] 灵元 {key} 不足");
                }
            }
            //Trigger:触发消耗灵元事件

            return totalConsumed;
        }
        public void RecoverMana(int amount)
        {
            if(amount <= 0) return;
            Mp += amount;
        }
}
}