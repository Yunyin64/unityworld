using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        /// <summary>开场/本方轮末补灵入口，不再自带独立 CD。</summary>
        public void DoManaDraw(bool noCheck = false)
        {
            if (!IsActive) return;
            Scene.TriggerCombatEvent("OnBaseManaDraw", new APIContext { Caster = this, Scene = Scene });
            DrawMana((int)GetStat("ManaDrawCost"));
        }

        /// <summary>只按实际落入空格的数量支付 MP；每个灵元是一个有位置的实体。</summary>
        public void DrawMana(int planned)
        {
            if (!IsActive) return;
            var empty = Backpack.EmptyCells();
            var actual = Math.Min(Math.Max(0, planned), Math.Min(Math.Max(0, Mp), empty.Count));
            var affinity = GetAffinity();
            var weights = new (ElementType Element, int Weight)[]
            {
                (ElementType.Jin, Math.Max(0, affinity.Jin)),
                (ElementType.Mu, Math.Max(0, affinity.Mu)),
                (ElementType.Shui, Math.Max(0, affinity.Shui)),
                (ElementType.Huo, Math.Max(0, affinity.Huo)),
                (ElementType.Tu, Math.Max(0, affinity.Tu))
            };
            var total = weights.Sum(w => w.Weight);
            for (int i = 0; i < actual; i++)
            {
                var index = Scene.Soul.Random(0, empty.Count);
                var cell = empty[index];
                empty.RemoveAt(index);
                var chosen = weights[0].Element;
                if (total > 0)
                {
                    var roll = Scene.Soul.Random(0, total);
                    foreach (var item in weights)
                    {
                        roll -= item.Weight;
                        if (roll < 0) { chosen = item.Element; break; }
                    }
                }
                else chosen = weights[Scene.Soul.Random(0, weights.Length)].Element;
                Backpack.PlaceMana(cell, chosen);
                Log($"生成灵元 {chosen} → 格({cell.X},{cell.Y})");
            }
            if (actual > 0) ChangeMP(-actual);
            Log($"补灵 计划={planned} 实际={actual} MP成本={actual} 剩余MP={Mp}");
            if (actual > 0) Scene.TriggerCombatEvent("OnManaDraw", new APIContext { Caster = this, Scene = Scene });
        }

        // 旧转换/减灵特殊卡尚待内容重设计。明确拒绝旧入口，不能用聚合字典重建资源池。
        public void ManaConvert(Dictionary<ElementType, int> cost) => throw new NotSupportedException("旧灵元转换卡需按背包位置重新设计。");
        public int ConsumeMana(Dictionary<ElementType, int> cost) => throw new NotSupportedException("不存在灵元池扣费，请通过背包扫描点亮卡牌。");
        public bool CanAffordMana(Dictionary<ElementType, int> cost) => throw new NotSupportedException("不存在灵元池资源检查。");
        public bool TryCostMana(Dictionary<ElementType, int> cost) => throw new NotSupportedException("不存在灵元池扣费。");
        public int GetManaCount(ElementType element) => Backpack.Mana.Values.Count(e => e.Equals(element));
        public IEnumerable<ElementType> GetUnassignedElements() => Backpack.Mana.Values.Distinct().ToArray();

        public int RecoverMP(int amount)
        {
            int final = Math.Min(amount, GetCombatMpMax() - Mp);
            Log($"回复{final}MP ");
            ChangeMP(final);
            return final;
        }
        public int ReduceMP(int amount)
        {
            int final = Math.Min(amount, Mp);
            Log($"消耗{final}MP ");
            ChangeMP(-final);
            return final;
        }
        private void ChangeMP(int amount)
        {
            Mp += amount;
            Scene.TriggerCombatEvent("OnMPChanged", new APIContext { Caster = this, Scene = Scene });
        }

        public string RandomBaseElementBuff(bool isDebuff = false)
        {
            return ElementType.RandomBaseElementBuff(Scene.Soul,isDebuff);
        }
}
}