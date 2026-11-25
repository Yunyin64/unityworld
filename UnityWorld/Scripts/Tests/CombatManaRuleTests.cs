using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

/// <summary>独立费用样例与正式结算入口的战斗回归。</summary>
public static class CombatManaRuleTests
{
    private const string Exhausted = "card_wound_mana_exhaustion";

    /// <summary>验证基础供灵、费用优先级、出手元素和卡牌归属。</summary>
    public static void Run()
    {
        Elements();
        foreach (int mp in new[] { 0, 2, 3 }) OpeningDraw(mp, 3);
        OpeningDraw(0, 0);
        OpeningDraw(5, 0);
        FeesAndPriority();
        ArtifactPower();
        Settlement();
        Console.WriteLine("COMBAT_MANA_RULES_OK");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("灵元规则：" + message);
    }

    private static Npc Character(int draw = 0)
    {
        var npc = BackpackModelTests.CreateNpc();
        npc.Stats.AddModifier("ManaDrawCost", StatModifier.Override(draw, "费用验收"));
        return npc;
    }

    private static Card Fixture(Npc npc, string id, int size, Dictionary<string, int> cost,
        List<string> keywords, int x, int y)
    {
        var card = CardMgr.Instance.InstantiateFromDefine(new CardDefine
        {
            ID = id, Size = size, Keywords = keywords, ManaCost = cost, ProducesContest = false
        });
        npc.GainCard(card);
        npc.PlaceBackpackCardInternal(card.Id, x, y);
        return card;
    }

    private static CombatScene Start(Npc a, Npc b)
    {
        var scene = new CombatScene();
        scene.Init(4711, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 180);
        scene.PreStart();
        scene.Start();
        return scene;
    }

    private static bool Charge(CombatNpc owner, CombatCard card, ElementType element)
    {
        var cell = owner.Backpack.EmptyCells()[0];
        owner.Backpack.PlaceMana(cell, element);
        return owner.Backpack.TryAssign(cell, card);
    }

    private static void Elements()
    {
        // 下标是木火土金水的位集，预期值独立列出以防算法和测试使用同一个推导。
        var cycle = new[] { ElementType.Mu, ElementType.Huo, ElementType.Tu, ElementType.Jin, ElementType.Shui };
        string[] expected = {
            "None", "Mu", "Huo", "Huo", "Tu", "Mu", "Tu", "Tu",
            "Jin", "Jin", "Huo", "Huo", "Jin", "Jin", "Jin", "Jin",
            "Shui", "Mu", "Shui", "Huo", "Tu", "Mu", "Tu", "Tu",
            "Shui", "Mu", "Shui", "Huo", "Shui", "Mu", "Shui", "Tu"
        };
        for (int mask = 0; mask < expected.Length; mask++)
        {
            var costs = cycle.Where((_, i) => (mask & (1 << i)) != 0).ToDictionary(e => e, _ => 1);
            costs[ElementType.None] = 9;
            Check(ElementType.FromManaCost(costs).ToString() == expected[mask]
                && ElementType.FromManaCost(costs.Reverse().ToDictionary()).ToString() == expected[mask], $"五行组合 {mask}");
        }
        Check(ElementType.FromManaCost(new Dictionary<ElementType, int>()).Equals(ElementType.None), "空费用");
        var unequal = cycle.ToDictionary(e => e, _ => 1);
        unequal[ElementType.Mu] = 2;
        Check(ElementType.FromManaCost(unequal).Equals(ElementType.Mu), "五色独高取木");
    }

    private static void OpeningDraw(int mp, int free)
    {
        var a = Character(5);
        var b = Character();
        var before = new DelegateEventListener((_, _, args) =>
        {
            var owner = ((APIContext)args).Caster;
            if (owner.Id != a.Id || owner.Backpack.Round != 1) return;
            owner.ReduceMP(owner.GetMp() - mp);
            foreach (var cell in owner.Backpack.EmptyCells().Skip(free))
                owner.Backpack.PlaceMana(cell, ElementType.Tu);
        });
        EventMgr.Instance.RegisterEvent("mana-rule-open", "OnBaseManaDraw", ScopeKey.Global, before);
        try
        {
            var scene = Start(a, b);
            try
            {
                var owner = scene.GetCombatants()[a.Id];
                int wounds = free > mp ? 1 : 0;
                int generated = Math.Min(mp, free);
                var exhaustion = owner.GetField().Where(c => c.DefineId == Exhausted).ToArray();
                Check(owner.GetMp() == mp - generated && owner.GetSp() == 1 + wounds
                    && exhaustion.Length == wounds, "开场空格/余额边界");
                Check(exhaustion.All(card => CardMgr.Instance.GetById(card.Id) == null), "发伤口时只创建战斗实例");
                Check(owner.Backpack.EmptyCells().Count == free - generated - wounds, "只扣实际生成数，伤口占一格");
                if (mp == 0 && free == 3)
                {
                    owner.DrawMana(5);
                    owner.DealFieldChange();
                    Check(owner.GetSp() == 2, "主动抽灵本身不致伤");
                    while (owner.Backpack.Round < 2) scene.Tick();
                    Check(owner.GetSp() == 3, "下一轮有空格而无 MP 再得一个伤口");
                }
            }
            finally { scene.Cleanup(); }
        }
        finally { EventMgr.Instance.RemoveEvent("OnBaseManaDraw", ScopeKey.Global, before); }
    }

    private static void FeesAndPriority()
    {
        var a = Character();
        var b = Character();
        var mixed = Fixture(a, "fee-mixed", 1, new() { ["Huo"] = 1, ["None"] = 1 }, ["FaShu"], 0, 1);
        var generic = Fixture(a, "fee-generic", 1, new() { ["None"] = 1 }, ["FaShu"], 1, 0);
        var cheap = Fixture(a, "fee-large", 3, new() { ["Huo"] = 1 }, ["FaShu"], 2, 3);
        var costly = Fixture(a, "fee-small", 1, new() { ["Mu"] = 2 }, ["FaShu"], 2, 2);

        var scene = Start(a, b);
        var paid = new List<APIContext>();
        var listener = new DelegateEventListener((_, _, args) =>
        {
            var ctx = (APIContext)args;
            if (ctx.SourceCard.Id == mixed.Id) paid.Add(ctx);
        });
        EventMgr.Instance.RegisterEvent("generic-payment", "OnManaUsed", ScopeKey.Global, listener);
        try
        {
            var owner = scene.GetCombatants()[a.Id];
            CombatCard Find(Card c) => owner.GetField().Single(current => current.Id == c.Id);
            var source = new BackpackCell(2, 0);
            Check(owner.Backpack.SelectManaTarget(source, ElementType.Huo, [Find(generic), Find(cheap)]) == Find(cheap),
                "远处缺火先于近处通用");
            Check(Charge(owner, Find(cheap), ElementType.Huo)
                && owner.Backpack.SelectManaTarget(source, ElementType.Huo, [Find(generic), Find(cheap)]) == Find(generic),
                "指定色填满之后才考虑通用");
            Check(Charge(owner, Find(costly), ElementType.Mu) && !Find(costly).HasActivationResources()
                && Charge(owner, Find(costly), ElementType.Mu), "一格卡可接收两枚木");
            var card = Find(mixed);
            Check(Charge(owner, card, ElementType.Mu) && !card.CanAcceptMana(ElementType.Mu)
                && Charge(owner, card, ElementType.Huo) && card.HasActivationResources(), "木先填通用，火留给本色");
            var ctx = new APIContext { Caster = owner, SourceCard = card, Scene = scene };
            ctx.Set("Domain", "Self"); ctx.Set("Element", "Huo"); ctx.Set("Amount", 1);
            CombatBaseFunc.StripCardMana(ctx);
            Check(!card.CanAcceptMana(ElementType.Tu) && Charge(owner, card, ElementType.Huo), "剥离火后只剩指定火缺口");
            Check(owner.GetSp() == 7 && Find(cheap).HasActivationResources(), "SP 仍按各卡真实体量");
            while (scene.CurrentTick < 30) scene.Tick();
            Check(Find(costly).UseCount == 1 && Find(cheap).UseCount == 0, "一格高费卡在本行发动");
            scene.Tick();
            while (scene.CurrentTick < 40) scene.Tick();
            Check(Find(cheap).UseCount == 1 && Find(cheap).LitCount == 0, "三格低费卡等到末格才发动并清空");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnManaUsed", ScopeKey.Global, listener);
            scene.Cleanup();
        }
        Check(paid.Count == 1 && paid[0].GetValue("PaidHuo", 0) == 1 && paid[0].GetValue("PaidMu", 0) == 1
            && paid[0].GetValue("PaidNone", 0) == 0, "通用费用不覆盖实际支付的火、木");
    }

    private static void ArtifactPower()
    {
        var a = Character();
        var b = Character();
        var generic = Fixture(a, "power-generic", 1, new() { ["None"] = 1 }, ["FaBao"], 1, 3);
        var exact = Fixture(a, "power-exact", 1, new() { ["Huo"] = 1 }, ["FaBao"], 2, 3);
        ElementType Replay()
        {
            var scene = Start(a, b);
            try
            {
                var owner = scene.GetCombatants()[a.Id];
                owner.AddModifier("ArtifactPower_Huo", 1);
                while (owner.Backpack.Round < 2) scene.Tick();
                var card = owner.GetField().Single(c => c.Id == generic.Id);
                Check(card.LitCount == 0 && owner.GetField().Single(c => c.Id == exact.Id).AssignedElements.Single().Equals(ElementType.Huo),
                    "直接供能优先给指定火");
                owner.RemoveModifier("ArtifactPower_Huo");
                owner.AddModifier("ArtifactPower_None", 2);
                while (owner.Backpack.Round < 3) scene.Tick();
                return card.AssignedElements.Single();
            }
            finally { scene.Cleanup(); }
        }
        var color = Replay();
        Check(color.IsBasic && color.Equals(Replay()), "不限色法宝供能按真实五行可重放");
    }

    private static void Settlement()
    {
        var a = Character(4);
        var b = Character();
        var oldWound = Fixture(a, "ordinary-wound", 1, new(), ["Passive", "Wound"], 1, 0);
        var oldWoundPosition = a.GetBackpackCardPosition(oldWound.Id);
        var tempSpell = new CardDefine
        {
            ID = "temp-spell", Size = 1, Keywords = ["FaShu", "Temp"], ManaCost = new() { ["Huo"] = 1 }
        };
        const System.Reflection.BindingFlags nonPublic = System.Reflection.BindingFlags.NonPublic;
        var createTemp = typeof(CombatCard).GetMethod("CreateFromDefine", nonPublic | System.Reflection.BindingFlags.Static)!;
        a.Stats.SetBase("ShenShi", 4);
        Check(!APIMgr.Instance.Contains("DestroyCard")
            && typeof(CombatCard).GetMethod("DestroyCard") == null, "销毁请求仅由关键词回调返回");
        using var probeLua = new NLua.Lua();
        using var ownHook = (NLua.LuaTable)probeLua.DoString(
            "return { OnLeave = function(self, ctx) self.called = true; return true end }")[0];
        CombatCard survivingWound = null!;
        int earlyId = -1;
        int stayedId = -1;
        var opening = new DelegateEventListener((_, _, args) =>
        {
            var ctx = (APIContext)args;
            var owner = ctx.Caster;
            if (owner.Id != a.Id || owner.Backpack.Round != 1) return;
            owner.ReduceMP(owner.GetMp());
            var ordinary = owner.GetField().Single(c => c.Id == oldWound.Id);
            ordinary.env = ownHook;
            ordinary.ScanLuaHooks();
            ctx.Set("Domain", "Self"); ctx.Set("Size", 1); ctx.Set("Exact", true);
            CombatBaseFunc.RemoveRandomWound(ctx);
            var temp = (CombatCard)createTemp.Invoke(null, [tempSpell])!;
            var stayed = (CombatCard)createTemp.Invoke(null, [tempSpell])!;
            earlyId = temp.Id;
            stayedId = stayed.Id;
            owner.AddCombatCard(temp);
            owner.AddCombatCard(stayed);
            owner.DealFieldChange();
            Check(CardMgr.Instance.GetById(earlyId) == null && CardMgr.Instance.GetById(stayedId) == null,
                "临时法术入场时没有世界实例");
            owner.RemoveCombatCard(temp);
            owner.DealFieldChange();
            Check(!owner.GetField().Contains(temp) && !owner.Backpack.Placements.ContainsKey(earlyId)
                && owner.GetAllCards().Contains(temp), "离场先释放占格，实例留到下一 Tick 清理");
            owner.HandleHpZero(new DamageInfo(owner, null, owner, 1));
            owner.DealFieldChange();
            var newWound = owner.GetField().Single(c => c.HasKeyword("Wound"));
            Check(newWound.DefineId != Exhausted && CardMgr.Instance.GetById(newWound.Id) == null,
                "普通轻伤直接创建为战斗实例");
            CombatBaseFunc.RemoveRandomWound(ctx);
            owner.DealFieldChange();
            survivingWound = owner.AddWound(newWound.DefineId);
            owner.DealFieldChange();
        });
        EventMgr.Instance.RegisterEvent("mana-settle", "OnRoundStart", ScopeKey.Global, opening);
        try
        {
            var held = a.GetAllCardIds().ToArray();
            var trial = Start(a, b);
            try
            {
                var owner = trial.GetCombatants()[a.Id];
                var exhausted = owner.GetField().Single(c => c.DefineId == Exhausted);
                Check(owner.GetSp() == owner.GetSpMax() && owner.IsActive, "首张临时伤口在场时计入 SP");
                trial.Tick();
                Check(!owner.GetAllCardIds().Contains(earlyId) && owner.GetAllCards().All(c => c.Id != earlyId),
                    "下一 Tick 从本场卡组清除临时实例和编号");
                Check(ownHook["called"] is true && owner.GetAllCardIds().Contains(oldWound.Id)
                    && owner.GetAllCards().Any(c => c.Id == oldWound.Id), "普通卡离场后仍保留战斗实例");
                while (!trial.IsFinished) trial.Tick();
                Check(owner.Status == CombatantStatus.Defeated, "新增临时伤口先触发 SP 判负");
                Check(owner.GetAllCards().All(c => !c.HasKeyword("Temp"))
                    && !owner.GetAllCardIds().Contains(stayedId) && !owner.GetAllCardIds().Contains(exhausted.Id)
                    && !owner.Backpack.Placements.ContainsKey(stayedId) && !owner.Backpack.Placements.ContainsKey(exhausted.Id),
                    "战末清除仍在场的临时实例、编号和占格");
            }
            finally { trial.Cleanup(); }
            Check(held.SequenceEqual(a.GetAllCardIds()), "试战不改持有或备战");
            var result = CombatMgr.Instance.RunCombat(a, b);
            var retained = a.GetAllCards().Where(c => c.HasKeyword("Wound")).ToArray();
            Check(retained.Length == 2 && retained.Contains(oldWound)
                && Equals(a.GetBackpackCardPosition(oldWound.Id), oldWoundPosition)
                && CardMgr.Instance.GetById(oldWound.Id) == oldWound,
                "在战中治愈的旧伤仍由世界角色持有并保留原卡位");
            var fresh = retained.Single(card => card != oldWound);
            Check(fresh.DefineId == survivingWound.DefineId && fresh.Id != survivingWound.Id
                && CardMgr.Instance.GetById(survivingWound.Id) == null,
                "两张同定义新伤治愈一张，世界只新建一张");
            foreach (int id in new[] { earlyId, stayedId })
                Check(!a.GetAllCardIds().Contains(id) && a.GetBackpackCardPosition(id) == null
                    && CardMgr.Instance.GetById(id) == null, "临时卡未带回世界");
            var commit = typeof(CombatResult).GetMethod("CommitToWorld",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            commit!.Invoke(result, [new[] { (a, CombatTeam.TeamA), (b, CombatTeam.TeamB) }]);
            Check(a.GetAllCards().Count(c => c.HasKeyword("Wound")) == 2, "重复提交伤口不翻倍");
        }
        finally { EventMgr.Instance.RemoveEvent("OnRoundStart", ScopeKey.Global, opening); }
    }
}
