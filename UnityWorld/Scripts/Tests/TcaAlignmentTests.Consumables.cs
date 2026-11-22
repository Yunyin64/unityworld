using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

public static partial class TcaAlignmentTests
{
    private static void VerifyAmmoInstances()
    {
        var definition = new CardDefine { ID = "bp_focus", AmountMax = 3, Keywords = ["FaShu", "Amount"] };
        var worldCard = CardMgr.Instance.InstantiateFromDefine(definition);
        worldCard.Stats.AddModifier("AmountMaxAdd", StatModifier.Flat(1, "world-ammo-test"));
        var first = CombatCard.CreateFromData(worldCard);
        var second = CombatCard.CreateFromData(CardMgr.Instance.InstantiateFromDefine(definition));
        first.AddCardBuff(CardModifier.StatBuff("ammo-test", "AmountMaxAdd", 1));
        first.ConsumeAmount(2);
        first.ConsumeAmount(-1);
        Check(first.CurrentAmount == 2 && first.GetAmountMax() == 5 && second.CurrentAmount == 3,
            "同名卡各有余弹，有效上限含修正，非正扣量无效");
        first.ConsumeAmount(int.MaxValue);
        Check(first.IsAmount && !first.HasAmount && first.RefillAmount(int.MaxValue) == 5 &&
            first.RefillAmount(1) == 0 && CombatCard.CreateFromData(worldCard).CurrentAmount == 4,
            "空弹可补至上限，新战斗从满弹开始");
        definition.Keywords = ["FaShu"];
        var plain = CombatCard.CreateFromData(CardMgr.Instance.InstantiateFromDefine(definition));
        plain.ConsumeAmount();
        Check(!plain.IsAmount && plain.CurrentAmount == -1 && plain.RefillAmount(3) == 0,
            "普通卡不产生弹药");
    }

    private static void VerifyAmmoScan(Lua lua, bool flexible)
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "ammo-scan"));
        b.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "ammo-scan"));
        var world = BackpackModelTests.Place(a, "bp_focus", 1, 0);
        world.BaseData.Keywords = ["FaBao", "Amount"];
        if (flexible) world.BaseData.Keywords.Add("Flexible");
        world.BaseData.AmountMax = 3;
        var right = BackpackModelTests.Place(a, "bp_focus", 2, 0);
        right.BaseData.StackMax = 3;
        var attack = BackpackModelTests.Place(a, "bp_attack", 1, 1);
        attack.BaseData.Keywords.Add("Amount");
        attack.BaseData.AmountMax = 1;
        var scene = new CombatScene();
        using var refill = lua.GetFunction("RefillAmmo");
        using var empty = lua.GetFunction("CheckAmmoExhausted");
        using var keyword = lua.GetFunction("HasKeyword");
        var exits = new List<(int Card, bool Freed)>();
        var lastAmmo = new List<int>();
        var leave = new DelegateEventListener((_, _, args) =>
        {
            var ctx = (APIContext)args;
            exits.Add((ctx.SourceCard.Id, ctx.Caster.Backpack.GetCardCells(ctx.SourceCard.Id).Count == 0 &&
                !ctx.Caster.GetField().Contains(ctx.SourceCard)));
        });
        var spent = new DelegateEventListener((_, _, args) => lastAmmo.Add(((APIContext)args).SourceCard.Id));
        EventMgr.Instance.RegisterEvent("ammo-flex", "OnFlexible", ScopeKey.Global, leave);
        EventMgr.Instance.RegisterEvent("ammo-empty", "OnAmmoExhausted", ScopeKey.Global, spent);
        scene.Init(7358, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 300);
        try
        {
            scene.PreStart();
            scene.Start();
            var owner = scene.GetCombatants()[a.Id];
            CombatCard Find(Card original) => owner.GetField().Single(card => card.Id == original.Id);
            var artifact = Find(world);
            var spell = Find(attack);
            void Charge(CombatCard card)
            {
                var cell = owner.Backpack.EmptyCells()[0];
                var element = card.GetManaCost().Keys.Single();
                owner.Backpack.PlaceMana(cell, element);
                Check(owner.Backpack.TryAssign(cell, card), "匹配灵元可交给在场卡");
            }
            void To(int tick)
            {
                while (!scene.IsFinished && scene.CurrentTick < tick) scene.Tick();
            }
            var query = new APIContext { Caster = owner, SourceCard = artifact, Scene = scene };
            using var script = (LuaTable)lua.DoString("return {OnAmmoProbe = function(self,ctx) self.hits = (self.hits or 0) + 1 end}")[0];
            artifact.env = script;
            artifact.ScanLuaHooks();
            Check(!Find(right).HasConsume, "只配置 StackMax 不会在战斗中派生 Consume");
            Charge(spell);
            spell.Freeze();
            foreach (int tick in new[] { 10, 60, 110 })
            {
                Charge(artifact);
                To(tick);
                Check(artifact.Phase == CombatCardPhase.Active && artifact.CurrentAmount == 2 - (tick - 10) / 50,
                    "激活或成功续期只支付一发，最后一发保持 Active");
                if (tick == 10)
                {
                    for (int i = 0; i < 3; i++) scene.TriggerCombatEvent("OnAmmoProbe", query);
                    Check(Convert.ToInt32(script["hits"]) == 3 && artifact.CurrentAmount == 2,
                        "法宝激活期内每次响应卡面效果都不再扣弹");
                    To(20);
                    Check(spell.CurrentAmount == 1 && spell.LitCount == 1 && spell.UseCount == 0, "冻结扫描不扣弹");
                }
                if (tick == 60)
                {
                    To(70);
                    Check(owner.PendingSlot?.SourceCard == spell && spell.CurrentAmount == 0, "法术入槽付一发");
                }
            }
            Check(spell.ApplyCount == 1, "零弹仍打完最后一击");
            Charge(artifact);
            Charge(Find(right));
            Check((bool)empty.Call(query, artifact)[0] && !(bool)empty.Call(query, Find(right))[0] &&
                (bool)keyword.Call(query, artifact, "Amount")[0] && !(bool)keyword.Call(query, Find(right), "Amount")[0],
                "空弹、无弹药机制、关键词条件均针对显式传入的卡");
            artifact.BaseData.Keywords.Add("ModProbe");
            Check((bool)keyword.Call(query, artifact, "ModProbe")[0] && !(bool)keyword.Call(query, spell, "ModProbe")[0],
                "Mod 关键词条件区别本卡与事件来源");
            artifact.BaseData.Keywords.Remove("ModProbe");
            Check(lastAmmo.Count(id => id == artifact.Id) == 1 && exits.Count == 0,
                "最后一次续期只报弹药耗尽，仍在场");
            if (flexible)
            {
                refill.Call(query, "Self", 1);
                Check(!(bool)empty.Call(query, artifact)[0] && artifact.LitCount == 1, "补弹立即解除空弹，蓄灵保留");
                To(160);
                Check(artifact.Phase == CombatCardPhase.Active && artifact.CurrentAmount == 0 &&
                    lastAmmo.Count(id => id == artifact.Id) == 2 && exits.Count == 0, "到期前补一发，继续完整维持一期");
                Charge(artifact);
                To(210);
                Check(exits.SequenceEqual([(artifact.Id, true)]) && Find(right).UseCount == 1,
                    "灵活法宝失活后实际离场，右侧卡能取得本行额度");
                return;
            }
            To(160);
            Check(artifact.LitCount == 1 && artifact.Phase == CombatCardPhase.Fulling &&
                Find(right).UseCount == 1, "空弹法宝续期失败，保留点亮并给同行右侧卡留出额度");
            Check(artifact.RefillAmount(1) == 1 && artifact.Phase == CombatCardPhase.Fulling, "补弹不直接激活法宝");
            To(210);
            Check(artifact.UseCount == 4 && artifact.CurrentAmount == 0 && artifact.Phase == CombatCardPhase.Active,
                "补弹后的下一次成功扫描可再激活");
            owner.RemoveCombatCard(artifact);
            owner.DealFieldChange();
            owner.AddCombatCard(artifact);
            owner.DealFieldChange();
            Check(artifact.CurrentAmount == 0 && owner.GetField().Contains(artifact), "同场重入不会重新装满");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnFlexible", ScopeKey.Global, leave);
            EventMgr.Instance.RemoveEvent("OnAmmoExhausted", ScopeKey.Global, spent);
            scene.Cleanup();
        }
    }

    private static void VerifyRemovalOrder(Lua lua)
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        // 两格法术：事件监听时可直接核实两个格子和 2 SP 都已腾出。
        var source = CardMgr.Instance.InstantiateFromDefine(new CardDefine
        {
            ID = "bp_attack", Size = 2, ManaCost = new() { ["Jin"] = 2 },
            Keywords = ["FaShu", "Amount", "Flexible"], AmountMax = 1
        });
        a.GainCard(source);
        a.PlaceBackpackCardInternal(source.Id, 1, 1);
        var listener = BackpackModelTests.Place(a, "bp_focus", 4, 3);
        var scene = new CombatScene();
        scene.Init(7360, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 30);
        var reasons = new List<string>();
        var releases = new List<(int Sp, int Cells)>();
        var leave = new DelegateEventListener((_, _, args) => reasons.Add(((APIContext)args).GetStringValue("RemovalReason")));
        var flexible = new DelegateEventListener((_, _, args) =>
        {
            var ctx = (APIContext)args;
            releases.Add((ctx.Caster.GetSp(), ctx.Caster.Backpack.GetCardCells(ctx.SourceCard.Id).Count));
        });
        EventMgr.Instance.RegisterEvent("removal-order", "OnCardLeave", ScopeKey.Global, leave);
        EventMgr.Instance.RegisterEvent("flex-order", "OnFlexible", ScopeKey.Global, flexible);
        try
        {
            scene.PreStart();
            scene.Start();
            var owner = scene.GetCombatants()[a.Id];
            var card = owner.GetField().Single(c => c.Id == source.Id);
            var ctx = new APIContext { Caster = owner, SourceCard = card, Scene = scene };
            // 与实卡脚本走同一场上事件分派；从 Lua 观察离场后的格子与 SP。
            var watcher = owner.GetField().Single(c => c.Id == listener.Id);
            using var watchScript = (LuaTable)lua.DoString("""
                return { OnFlexible = function(self, ctx)
                    self.sp = ctx.Caster:GetSp()
                    self.cells = ctx.Caster.Backpack:GetCardCells(ctx.SourceCard.Id).Count
                end }
                """)[0];
            watcher.env = watchScript;
            watcher.ScanLuaHooks();
            using var remove = lua.GetFunction("RemoveCard");
            int remainingSp = owner.GetSp() - 2;
            Check(owner.Backpack.GetCardCells(card.Id).Count == 2, "两格测试卡实际占两格");
            remove.Call(ctx, "Self", "Effect");
            card.ConsumeAmount();
            owner.DealFieldChange();
            Check(reasons.SequenceEqual(["Effect"]) && releases.Count == 0 && watchScript["sp"] == null,
                "外部移除先入队时，即使灵活也提交了请求，仍只报告普通离场");
            owner.AddCombatCard(card);
            owner.DealFieldChange();
            card.RefillAmount(1);
            card.ConsumeAmount();
            remove.Call(ctx, "Self", "Effect");
            Check(releases.Count == 0 && owner.GetSp() > remainingSp, "灵活的离场请求尚未释放 SP");
            owner.DealFieldChange();
            Check(reasons.SequenceEqual(["Effect", "Flexible"]) && releases.SequenceEqual([(remainingSp, 0)]) &&
                Convert.ToInt32(watchScript["sp"]) == remainingSp && Convert.ToInt32(watchScript["cells"]) == 0,
                "灵活先提交时只触发一次，监听者可使用已经腾出的格子和 SP");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnCardLeave", ScopeKey.Global, leave);
            EventMgr.Instance.RemoveEvent("OnFlexible", ScopeKey.Global, flexible);
            scene.Cleanup();
        }
    }

    private static void VerifyLastArtifactAtTimeout()
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        var source = BackpackModelTests.Place(a, "bp_focus", 1, 0);
        source.BaseData.Keywords = ["FaBao", "Amount", "Flexible"];
        source.BaseData.AmountMax = 1;
        var scene = new CombatScene();
        scene.Init(7362, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 11);
        try
        {
            scene.PreStart();
            scene.Start();
            var owner = scene.GetCombatants()[a.Id];
            var card = owner.GetField().Single(c => c.Id == source.Id);
            var cell = owner.Backpack.EmptyCells()[0];
            var element = card.GetManaCost().Keys.Single();
            owner.Backpack.PlaceMana(cell, element);
            owner.Backpack.TryAssign(cell, card);
            while (scene.CurrentTick < 10) scene.Tick();
            Check(card.Phase == CombatCardPhase.Active && card.CurrentAmount == 0 && owner.GetField().Contains(card),
                "最后一发激活后仍占位并生效");
            scene.Tick();
            Check(scene.IsFinished && !owner.GetField().Contains(card), "战斗在下次扫描前结束，灵活法宝完成离场");
        }
        finally { scene.Cleanup(); }
    }

    private static void VerifyDefeatedPending()
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        var source = BackpackModelTests.Place(a, "bp_attack", 1, 0);
        source.BaseData.Keywords.AddRange(["Amount", "Flexible"]);
        source.BaseData.AmountMax = 1;
        var scene = new CombatScene();
        scene.Init(7361, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 100);
        try
        {
            scene.PreStart();
            scene.Start();
            var owner = scene.GetCombatants()[a.Id];
            var card = owner.GetField().Single(c => c.Id == source.Id);
            var cell = owner.Backpack.EmptyCells()[0];
            var element = card.GetManaCost().Keys.Single();
            owner.Backpack.PlaceMana(cell, element);
            owner.Backpack.TryAssign(cell, card);
            while (scene.CurrentTick < 10) scene.Tick();
            Check(owner.PendingSlot?.SourceCard == card && owner.GetField().Contains(card), "耗尽后待发期间仍占场");
            owner.Stats.AddModifier("SpMax", StatModifier.Override(1, "defeat-test"));
            owner.CheckDefeated();
            scene.CheckEndConditions();
            Check(!owner.IsActive && owner.PendingSlot == null && card.ApplyCount == 0 &&
                !owner.GetField().Contains(card), "败退取消待发，交给原卡灵活收尾，不伪造 Apply");
        }
        finally { scene.Cleanup(); }
    }

    private static void VerifyConsumables(Lua lua)
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        b.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        BackpackModelTests.Place(b, "bp_guard", 1, 1);

        Card PlaceConsumable(string fixture, int y, params string[] keywords)
        {
            var card = CardMgr.Instance.InstantiateFromDefine(new CardDefine
            {
                ID = fixture, StackMax = 3, ManaCost = new() { ["Jin"] = 1 },
                Keywords = ["FaShu", "Consume", .. keywords]
            });
            Check(card.GetStack() == 3 && card.GetStackMax() == 3, "新消耗卡按定义上限初始化");
            card.BaseData.Stack = 1;
            a.GainCard(card);
            a.PlaceBackpackCardInternal(card.Id, 1, y);
            return card;
        }

        var flexible = PlaceConsumable("bp_attack", 0, "Flexible");
        var shieldItem = PlaceConsumable("bp_focus", 1, "Flexible");
        var ordinary = PlaceConsumable("bp_focus", 2);
        var reserve = PlaceConsumable("bp_focus", 3);
        a.UnassignCard(reserve.Id);
        reserve.BaseData.Stack = 3;
        a.AssignToReserve(reserve.Id);
        var consumed = new List<(int Id, int Paid, int Remaining)>();
        var exhausted = new List<(int Id, int Tick)>();
        var left = new List<(int Id, int Tick)>();
        using var checkConsume = lua.GetFunction("CheckConsumeCount");
        var useItem = new DelegateEventListener((_, _, args) =>
        {
            var ctx = (APIContext)args;
            consumed.Add((ctx.SourceCard.Id, ctx.GetValue("Amount", 0), ctx.GetValue("Stack", -1)));
        });
        var exhaustion = new DelegateEventListener((_, _, args) =>
        {
            var ctx = (APIContext)args;
            if (ctx.Caster.Id == a.Id && (bool)checkConsume.Call(ctx, ctx.SourceCard, "Equal", 0)[0])
                exhausted.Add((ctx.SourceCard.Id, ctx.Scene.CurrentTick));
        });
        var leaving = new DelegateEventListener((_, _, args) =>
        {
            var ctx = (APIContext)args;
            if (ctx.Caster.Id == a.Id && ctx.SourceCard.HasConsume)
                left.Add((ctx.SourceCard.Id, ctx.Scene.CurrentTick));
        });
        var opening = new DelegateEventListener((_, _, args) =>
        {
            var ctx = (APIContext)args;
            if (ctx.GetValue("Round", 0) != 1) return;
            ctx.Caster.GetReserve().SingleOrDefault(card => card.Id == reserve.Id)?.TryConsume();
            foreach (var card in ctx.Caster.GetField().Where(card => card.HasConsume || card.DefineId == "bp_guard"))
            {
                var cell = ctx.Caster.Backpack.EmptyCells()[0];
                var element = card.GetManaCost().Keys.Single();
                ctx.Caster.Backpack.PlaceMana(cell, element);
                ctx.Caster.Backpack.TryAssign(cell, card);
            }
        });
        EventMgr.Instance.RegisterEvent("tca-consume-pay", "OnConsumed", ScopeKey.Global, useItem);
        EventMgr.Instance.RegisterEvent("tca-consume-open", "OnRoundStart", ScopeKey.Global, opening);
        EventMgr.Instance.RegisterEvent("tca-consume-exhaust", "OnExhausted", ScopeKey.Global, exhaustion);
        EventMgr.Instance.RegisterEvent("tca-consume-leave", "OnCardLeave", ScopeKey.Global, leaving);
        var preview = new CombatScene();
        try
        {
            preview.Init(7356, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 90);
            preview.PreStart();
            preview.Start();
            var owner = preview.GetCombatants()[a.Id];
            var attack = owner.GetField().Single(card => card.Id == flexible.Id);
            var shield = owner.GetField().Single(card => card.Id == shieldItem.Id);
            var stay = owner.GetField().Single(card => card.Id == ordinary.Id);
            Check((bool)checkConsume.Call(new APIContext(), attack, "AtLeast", 1)[0] &&
                !(bool)checkConsume.Call(new APIContext(), attack, "AtLeast", 2)[0], "比较当前消耗次数");
            consumed.Clear(); // 开场候补扣份已经单独提交。
            var bulkWorld = CardMgr.Instance.InstantiateFromDefine(new CardDefine
            {
                ID = "bp_focus", StackMax = 3, Keywords = ["FaShu", "Consume"]
            });
            var bulk = CombatCard.CreateFromData(bulkWorld);
            bulk.Owner = owner;
            Check(bulk.TryConsume(2) == 2 && bulk.TryConsume(2) == 1 && bulk.TryConsume(2) == 0 &&
                consumed.SequenceEqual([(bulk.Id, 2, 1), (bulk.Id, 1, 0)]) && exhausted.Count == 1,
                "一次实扣只广播一次，耗尽另报一次，无效扣量不广播");
            consumed.Clear();
            exhausted.Clear();
            using var hasKeyword = lua.GetFunction("HasKeyword");
            var stockQuery = new APIContext { Scene = preview, SourceCard = bulk, Caster = owner };
            Check((bool)hasKeyword.Call(stockQuery, bulk, "Consume")[0] &&
                !(bool)checkConsume.Call(stockQuery, bulk, "AtLeast", 1)[0],
                "零份物品仍带消耗关键词，存量条件为假");
            stay.Freeze();
            for (int i = 0; i < 10; i++) preview.Tick();
            Check(attack.GetStack() == 0 && owner.PendingSlot?.SourceCard == attack && attack.ApplyCount == 0 &&
                owner.GetField().Contains(attack) && exhausted.SequenceEqual([(attack.Id, 10)]) && left.Count == 0 && consumed.SequenceEqual([(attack.Id, 1, 0)]),
                "最后一次先扣到零并耗尽，攻击待发期间仍在场");
            var freezeContext = new APIContext { Caster = owner, SourceCard = attack, Scene = preview };
            freezeContext.Set("Domain", "Self");
            CombatBaseFunc.Freeze(freezeContext);
            Check(!attack.IsFrozen && freezeContext.CardTargets.Count == 0,
                "耗尽仍在场的主动卡不会成为冻结目标");
            for (int i = 10; i < 30; i++) preview.Tick();
            Check(attack.ApplyCount == 1 && shield.ApplyCount == 1 && owner.ShieldValue == 2
                && left.ToHashSet().SetEquals([(attack.Id, 20), (shield.Id, 20)]) && left.Count == 2
                && !owner.GetField().Contains(attack) && owner.Backpack.GetCardCells(attack.Id).Count == 0,
                "灵活卡在最后一次实卡效果后离开，待发与当场效果均完整结算");
            Check(stay.GetStack() == 1 && stay.LitCount == 1 && stay.UseCount == 0 && !stay.IsFrozen,
                "被冻结的失败扫描保留次数与蓄灵");
            while (!preview.IsFinished) preview.Tick();
            var previewResult = preview.GetResult().Get<List<CombatantResult>>("Combatants").Single(value => value.NpcId == a.Id);
            Check(previewResult.Consumption.Single(entry => entry.CardId == shieldItem.Id) == new CombatConsumptionResult(shieldItem.Id, 1, 0, 0)
                && flexible.GetStack() == 1 && shieldItem.GetStack() == 1 && ordinary.GetStack() == 1,
                "试战记录实际耗用，世界物品仍保留开战份数");
            Check(stay.ApplyCount == 1 && stay.GetStack() == 0 && owner.GetField().Contains(stay),
                "非灵活耗尽后仍占格且只使用一次");

            var timedOut = new CombatScene();
            timedOut.Init(7357, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 10);
            timedOut.Run();
            var timeLimitOwner = timedOut.GetCombatants()[a.Id];
            Check(timeLimitOwner.PendingSlot == null && !timeLimitOwner.GetField().Any(card => card.Id == flexible.Id)
                && left.Count(entry => entry == (flexible.Id, 10)) == 1 && flexible.GetStack() == 1,
                "超时取消尚未配对的拼点，并清理零次灵活卡，试战仍不回写");

            var result = CombatMgr.Instance.RunCombat(a, b);
            Check(a.GetAllCards().Contains(flexible) && flexible.GetStack() == 0 && ordinary.GetStack() == 0
                && a.GetBackpackCardPosition(flexible.Id) == null && a.GetBackpackCardPosition(ordinary.Id) == null,
                "正式战斗提交耗尽卡的世界次数，只退出备战");
            Check(shieldItem.GetStack() == 0 && a.GetBackpackCardPosition(shieldItem.Id) == null
                && result.Get<List<CombatantResult>>("Combatants").Single(value => value.NpcId == a.Id)
                    .Consumption.Single(entry => entry.CardId == shieldItem.Id).FinalCount == 0,
                "即时护盾物品离场后按原实例提交库存，零份退出备战");

            Check(reserve.GetStack() == 2 && CombatCard.CreateFromData(reserve).ConsumeStack == 2,
                "实战保留剩余两份，下场从实际库存开始");

            bool RejectZero(Action attempt)
            {
                try { attempt(); return false; }
                catch (InvalidOperationException ex) { return ex.Message.Contains("不能加入备战"); }
            }
            Check(RejectZero(() => a.PlaceBackpackCardInternal(flexible.Id, 1, 0))
                && RejectZero(() => a.AssignToReserve(flexible.Id)), "零次消耗卡不能放回上场或候补");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnConsumed", ScopeKey.Global, useItem);
            EventMgr.Instance.RemoveEvent("OnRoundStart", ScopeKey.Global, opening);
            EventMgr.Instance.RemoveEvent("OnExhausted", ScopeKey.Global, exhaustion);
            EventMgr.Instance.RemoveEvent("OnCardLeave", ScopeKey.Global, leaving);
            preview.Cleanup();
        }
    }
}
