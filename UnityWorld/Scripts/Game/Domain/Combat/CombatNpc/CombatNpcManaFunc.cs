using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        internal void ChangeBattleAffinity(string element, int delta, int rounds)
        {
            if (element is not ("Jin" or "Mu" or "Shui" or "Huo" or "Tu") || delta == 0 || rounds < 0) return;
            var sign = delta > 0 ? "Up" : "Down";
            AddRoundModifier($"Affinity_{element}_{sign}", (int)Math.Min(int.MaxValue, Math.Abs((long)delta)), rounds);
        }

        internal void AddArtifactPower(ElementType element, int amount, int rounds)
        {
            AddRoundModifier($"ArtifactPower_{element.Kind}", amount, rounds);
        }

        private void PowerArtifacts()
        {
            var supplies = GetAllModifiers().Where(modifier => modifier.DefineId.StartsWith("ArtifactPower_", StringComparison.Ordinal)).ToArray();
            if (supplies.Length == 0) return;
            var artifacts = Field.Where(card => card.BaseData.MainKeyword == CardMainKeyword.FaBao)
                .OrderBy(card => card.Id).ToArray();
            foreach (var supply in supplies)
            {
                var supplyElement = ElementType.GetElementType(supply.DefineId["ArtifactPower_".Length..]);
                for (int i = 0; i < supply.CurrentStack; i++)
                {
                    // 先随机选择法宝，再从它仍可接收的匹配元素中随机选择一份。
                    var candidates = artifacts.Select(card => (Card: card, Elements: card.GetManaCost().Keys
                            .Where(element => (supplyElement.Equals(ElementType.None) || supplyElement.Equals(element))
                                && card.CanAcceptMana(element)).OrderBy(element => element.ToString(), StringComparer.Ordinal).ToArray()))
                        .Where(candidate => candidate.Elements.Length > 0).ToArray();
                    if (candidates.Length == 0) break;
                    var selected = candidates[Scene.Soul.Random(0, candidates.Length)];
                    var element = selected.Elements[Scene.Soul.Random(0, selected.Elements.Length)];
                    if (!selected.Card.TryReceiveMana(element)) continue;
                    Scene.MarkPresentationEffect();
                    selected.Card.DispatchManaCharged(element);
                }
            }
        }

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
                //Log($"生成灵元 {chosen} → 格({cell.X + 1},{cell.Y + 1})");
            }
            if (actual > 0) ChangeMP(-actual);
            Log($"补灵 计划={planned} 实际={actual} MP成本={actual} 剩余MP={Mp}");
            if (actual > 0) Scene.TriggerCombatEvent("OnManaDraw", new APIContext { Caster = this, Scene = Scene });
        }

        // 字典支付签名保留并拒绝；Convert/ReduceMana API直接操作背包格实体。
        public void ManaConvert(Dictionary<ElementType, int> cost) => throw new NotSupportedException("旧字典转换入口已禁用；请通过 Convert API 回收格上灵元。");
        public int ConsumeMana(Dictionary<ElementType, int> cost) => throw new NotSupportedException("不存在灵元池扣费，请通过背包扫描点亮卡牌。");
        public bool CanAffordMana(Dictionary<ElementType, int> cost) => throw new NotSupportedException("不存在灵元池资源检查。");
        public bool TryCostMana(Dictionary<ElementType, int> cost) => throw new NotSupportedException("不存在灵元池扣费。");
        public int GetManaCount(ElementType element) => Backpack.Mana.Values.Count(e => e.Equals(element));
        public IEnumerable<ElementType> GetUnassignedElements() => Backpack.Mana.Values.Distinct().ToArray();

        public int RecoverMP(int amount)
        {
            int final = Math.Min(Math.Max(0, amount), Math.Max(0, GetCombatMpMax() - Mp));
            Log($"回复{final}MP ");
            if (final > 0) ChangeMP(final);
            return final;
        }
        public int ReduceMP(int amount)
        {
            int final = Math.Min(Math.Max(0, amount), Math.Max(0, Mp));
            Log($"消耗{final}MP ");
            if (final > 0) ChangeMP(-final);
            return final;
        }
        private void ChangeMP(int amount)
        {
            Mp += amount;
            if (amount != 0) Scene?.MarkPresentationEffect();
            Scene.TriggerCombatEvent("OnMPChanged", new APIContext { Caster = this, Scene = Scene });
        }

        public string RandomBaseElementBuff(bool isDebuff = false)
        {
            return ElementType.RandomBaseElementBuff(Scene.Soul,isDebuff);
        }
}
}
