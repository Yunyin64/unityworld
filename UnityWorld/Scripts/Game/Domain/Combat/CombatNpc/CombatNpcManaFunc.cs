using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        /// <summary>按元素叠加指定轮数的亲和增减</summary>
        internal void ChangeBattleAffinity(string element, int delta, int rounds)
        {
            if (element is not ("Jin" or "Mu" or "Shui" or "Huo" or "Tu") || delta == 0 || rounds < 0) return;
            var sign = delta > 0 ? "Up" : "Down";
            AddRoundModifier($"Affinity_{element}_{sign}", (int)Math.Min(int.MaxValue, Math.Abs((long)delta)), rounds);
        }

        /// <summary>登记指定轮数的法宝元素供能</summary>
        internal void AddArtifactPower(ElementType element, int amount, int rounds)
        {
            AddRoundModifier($"ArtifactPower_{element.Kind}", amount, rounds);
        }

        /// <summary>按卡牌编号顺序向场上法宝提供灵元</summary>
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
                    // 先补指定色。随机先选法宝，再选它可接收的颜色。
                    var options = artifacts.SelectMany(card =>
                    {
                        IEnumerable<ElementType> colors = supplyElement.Equals(ElementType.None)
                            ? card.GetManaCost().Keys.Concat(ElementType.GeneratingCycle).Distinct()
                            : new[] { supplyElement };
                        return colors.OrderBy(e => e.ToString(), StringComparer.Ordinal)
                            .Where(card.CanAcceptMana)
                            .Select(e => (Card: card, Element: e, NeedsColor: card.NeedsSpecificMana(e)));
                    }).ToArray();
                    if (options.Length == 0) break;
                    bool hasSpecificCost = options.Any(o => o.NeedsColor);
                    var candidates = options.Where(o => o.NeedsColor == hasSpecificCost).GroupBy(o => o.Card).ToArray();
                    var group = candidates[Scene.Soul.Random(0, candidates.Length)].ToArray();
                    var selected = group[Scene.Soul.Random(0, group.Length)];
                    var element = selected.Element;
                    if (!selected.Card.TryReceiveMana(element)) continue;
                    Scene.MarkPresentationEffect();
                    selected.Card.DispatchManaCharged(element);
                }
            }
        }

        internal const string ManaExhaustionWoundId = "card_wound_mana_exhaustion";

        /// <summary>开场/本方轮末补灵入口，不再自带独立 CD。</summary>
        public void DoManaDraw(bool noCheck = false)
        {
            if (!IsActive) return;
            Scene.TriggerCombatEvent("OnBaseManaDraw", new APIContext { Caster = this, Scene = Scene });
            DrawMana((int)GetStat("ManaDrawCost"), true);
        }

        /// <summary>只按实际落入空格的数量支付 MP；每个灵元是一个有位置的实体。</summary>
        public void DrawMana(int planned) => DrawMana(planned, false);

        // 仅基础补灵产生耗尽伤口，以抽取前的空格和余额计算本次缺口。
        private void DrawMana(int planned, bool basicDraw)
        {
            if (!IsActive) return;
            var empty = Backpack.EmptyCells();
            int due = Math.Min(Math.Max(0, planned), empty.Count);
            int available = Math.Max(0, Mp);
            int actual = Math.Min(due, available);
            bool exhausted = basicDraw && due > available;
            int emptyCount = empty.Count;
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
            if (exhausted) AddWound(ManaExhaustionWoundId);
            Log($"补灵 计划={planned} 空格={emptyCount} 应补={due} 支付前MP={available} 实际={actual} MP成本={actual} 剩余MP={Mp} 耗尽伤口={exhausted}");
            if (actual > 0) Scene.TriggerCombatEvent("OnManaDraw", new APIContext { Caster = this, Scene = Scene });
        }

        // 字典支付签名保留并拒绝；Convert/ReduceMana API直接操作背包格实体。
        /// <summary>拒绝旧式灵元池字典回收入口</summary>
        public void ManaConvert(Dictionary<ElementType, int> cost) => throw new NotSupportedException("旧字典转换入口已禁用；请通过 Convert API 回收格上灵元。");
        /// <summary>拒绝旧式灵元池扣费</summary>
        public int ConsumeMana(Dictionary<ElementType, int> cost) => throw new NotSupportedException("不存在灵元池扣费，请通过背包扫描点亮卡牌。");
        /// <summary>拒绝旧式灵元池余额检查</summary>
        public bool CanAffordMana(Dictionary<ElementType, int> cost) => throw new NotSupportedException("不存在灵元池资源检查。");
        /// <summary>拒绝旧式灵元池试扣费</summary>
        public bool TryCostMana(Dictionary<ElementType, int> cost) => throw new NotSupportedException("不存在灵元池扣费。");
        /// <summary>统计盘面上尚未分配的指定元素灵元</summary>
        public int GetManaCount(ElementType element) => Backpack.Mana.Values.Count(e => e.Equals(element));
        /// <summary>列出盘面上未分配的灵元元素种类</summary>
        public IEnumerable<ElementType> GetUnassignedElements() => Backpack.Mana.Values.Distinct().ToArray();

        /// <summary>直接回复 MP，结算回复量修正后返回实际回复点数。</summary>
        public int RecoverMP(int amount)
        {
            if (amount > 0 && Mp < GetCombatMpMax())
            {
                var ctx = new APIContext { Caster = this, Scene = Scene, NpcTargets = [this] };
                amount = ModifyAmount("MpRecovery", amount, ctx);
            }

            return RestoreMP(amount);
        }

        /// <summary>按给定点数增加 MP，受缺失量限制。灵元回收通过此入口按固定比例结算。</summary>
        internal int RestoreMP(int amount)
        {
            int final = Math.Min(Math.Max(0, amount), Math.Max(0, GetCombatMpMax() - Mp));
            Log($"回复{final}MP ");
            if (final > 0) ChangeMP(final);
            return final;
        }
        /// <summary>从当前蓝条扣除至多指定量并返回实扣值</summary>
        public int ReduceMP(int amount)
        {
            int final = Math.Min(Math.Max(0, amount), Math.Max(0, Mp));
            Log($"消耗{final}MP ");
            if (final > 0) ChangeMP(-final);
            return final;
        }
        /// <summary>调整蓝条并派发蓝条变化事件</summary>
        private void ChangeMP(int amount)
        {
            Mp += amount;
            if (amount != 0) Scene?.MarkPresentationEffect();
            var ctx = new APIContext { Caster = this, Scene = Scene };
            ctx.Set("Before", Mp - amount);
            ctx.Set("After", Mp);
            ctx.Set("Delta", amount);
            Scene.TriggerCombatEvent("OnMPChanged", ctx);
        }

        /// <summary>随机选取一种基础五行增益或减益编号</summary>
        public string RandomBaseElementBuff(bool isDebuff = false)
        {
            return ElementType.RandomBaseElementBuff(Scene.Soul,isDebuff);
        }
}
}
