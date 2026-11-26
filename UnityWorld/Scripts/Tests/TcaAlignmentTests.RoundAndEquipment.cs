using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

public static partial class TcaAlignmentTests
{
    private static void VerifyNextStabOnScan(Lua lua)
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        b.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        var source = BackpackModelTests.Place(a, "bp_attack", 1, 0);
        var scene = new CombatScene();
        scene.Init(7359, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 200);
        try
        {
            scene.PreStart();
            scene.Start();
            var owner = scene.GetCombatants()[a.Id];
            var card = owner.GetField().Single(current => current.Id == source.Id);
            card.env = (LuaTable)lua.DoString("return {Contest=function(self, ctx) Attack(ctx, 'Jin', 'Ci', 4) end}")[0];
            card.ScanLuaHooks();
            using var add = lua.GetFunction("AddNpcBuff");
            var ctx = new APIContext { Caster = owner, SourceCard = card, Scene = scene };
            add.Call(ctx, "Self", "Ci_Changed_Once", 2, -1);
            var cell = owner.Backpack.EmptyCells()[0];
            owner.Backpack.PlaceMana(cell, ElementType.Jin);
            Check(owner.Backpack.TryAssign(cell, card), "一次性刺击验收卡实际点亮");
            for (int tick = 0; tick < 10; tick++) scene.Tick();
            Check(owner.PendingSlot?.SourceCard == card && owner.PendingSlot.ContestValue == 6 &&
                owner.GetAllModifiers().All(mod => mod.DefineId != "Ci_Changed_Once"),
                "刺击 4 在末格生成时变成 6 入槽，首次配对前一次性强化已经移除");

            add.Call(ctx, "Self", "Ci_Changed_Once", -3, -1);
            Check(owner.PendingSlot.ContestValue == 6 && owner.GetAllModifiers().Any(mod => mod.DefineId == "Ci_Changed_Once"),
                "入槽后挂的一次性弱化等待下一次生成匹配拼点");
        }
        finally
        {
            scene.Cleanup();
        }
    }

    private static void VerifyRoundEffects(Lua lua)
    {
        var shortCycle = BackpackModelTests.CreateNpc("bp_main_8x3");
        var longCycle = BackpackModelTests.CreateNpc("bp_main_6x5");
        shortCycle.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        longCycle.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        var scene = new CombatScene();
        scene.Init(7352, [(shortCycle, CombatTeam.TeamA), (longCycle, CombatTeam.TeamB)], 200);
        var owner = scene.GetCombatants()[shortCycle.Id];
        var opponent = scene.GetCombatants()[longCycle.Id];
        var events = new List<(int Id, string Name, int Round)>();
        var listener = new DelegateEventListener((_, _, args) =>
        {
            var data = (APIContext)args;
            events.Add((data.Caster.Id, data.EventName, data.GetValue("Round", 0)));
        });
        EventMgr.Instance.RegisterEvent("tca-round-start", "OnRoundStart", ScopeKey.Global, listener);
        EventMgr.Instance.RegisterEvent("tca-round-end", "OnRoundEnd", ScopeKey.Global, listener);
        try
        {
            scene.PreStart();
            scene.Start();
            Check(events.Count == 2 && events.Contains((owner.Id, "OnRoundStart", 1))
                && events.Contains((opponent.Id, "OnRoundStart", 1)), "开场双方独立派发第 1 轮开始");

            var ctx = new APIContext { Caster = owner, Scene = scene };
            using var affinity = lua.GetFunction("ChangeAffinity");
            affinity.Call(ctx, "Self", "Mu", 1, 2);
            for (int i = 0; i < 30; i++) scene.Tick();
            Check(events.Skip(2).SequenceEqual([(owner.Id, "OnRoundEnd", 1), (owner.Id, "OnRoundStart", 2)]),
                "三行角色先结束旧轮并进入新轮，五行角色仍在首轮");
            Check(owner.GetAllModifiers().Single(modifier => modifier.DefineId == "Affinity_Mu_Up").RemainingRounds == 1,
                "亲和加值可以按独立轮次持续");
            owner.Backpack.Charge(60);
            scene.Tick();
            Check(owner.Backpack.Round == 4 && opponent.Backpack.Round == 1, "单 Tick 跨两轮仍走每轮入口");
            Check(owner.GetAllModifiers().All(modifier => modifier.DefineId != "Affinity_Mu_Up"), "跨轮时移除到期 Buff");
            Check(events.TakeLast(4).SequenceEqual([(owner.Id, "OnRoundEnd", 2), (owner.Id, "OnRoundStart", 3),
                (owner.Id, "OnRoundEnd", 3), (owner.Id, "OnRoundStart", 4)]), "跨轮边界依次派发实际轮次");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnRoundStart", ScopeKey.Global, listener);
            EventMgr.Instance.RemoveEvent("OnRoundEnd", ScopeKey.Global, listener);
            scene.Cleanup();
        }
    }

    private static void VerifyEquipmentAndAffinity(Lua lua)
    {
        var a = NpcMgr.Instance.Assemble(new NpcDefine
        {
            ID = "tca-equipment", MainGongFaId = "jin_fa",
            InitStat = new() { ["ShenShi"] = 12, ["QiXue"] = 10, ["QiGan"] = 20, ["TiPo"] = 10 },
            InitGongFa = [new() { DefineId = "jin_fa", CurrentPoint = 100, PlacementIndex = 1 }],
            InitEquip =
            [
                new() { CardID = "card_fabao_jian", EquipID = "long_sword", PlacementIndex = 2 },
                new() { CardID = "card_fabao_jian", EquipID = "long_sword", PlacementIndex = 3 }
            ],
            InitCardPlacements =
            [
                new() { Key = "jin_fa", Value = new(3, 3) },
                new() { Key = "card_fabao_jian", Value = new(1, 4) },
                new() { Key = "card_fabao_jian", Value = new(4, 4) },
                new() { Key = "card_form_ci", Value = new(1, 1), ParentPlacementIndex = 2 },
                new() { Key = "card_form_zhan", Value = new(2, 1), ParentPlacementIndex = 2 },
                new() { Key = "card_form_ge", Value = new(1, 2), ParentPlacementIndex = 2 },
                new() { Key = "card_form_ci", Value = new(4, 1), ParentPlacementIndex = 3 },
                new() { Key = "card_form_zhan", Value = new(5, 1), ParentPlacementIndex = 3 },
                new() { Key = "card_form_ge", Value = new(4, 2), ParentPlacementIndex = 3 }
            ]
        });
        var b = BackpackModelTests.CreateNpc();
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(1, "tca-test"));
        b.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        foreach (var element in new[] { "Jin", "Mu", "Shui", "Huo", "Tu" })
            a.Stats.SetBase("Affinity" + element, element == "Jin" ? 1 : 0);

        var swords = a.GetAllCards().Where(card => card.DefineId == "card_fabao_jian").ToArray();
        var weapon = EquipDefineMgr.Instance.Get("long_sword");
        var raisedAttack = weapon.AttackBase + 2;

        swords[0].BaseData.ManaCost = new() { [ElementType.Mu] = 1, [ElementType.Huo] = 1, [ElementType.Tu] = 1 };
        swords[1].BaseData.ManaCost = new() { [ElementType.None] = 1 };
        var scene = new CombatScene();
        scene.Init(7353, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 200);
        try
        {
            scene.PreStart();
            var owner = scene.GetCombatants()[a.Id];
            var swordCard = owner.GetField().Single(card => card.Id == swords[0].Id);
            var thrust = owner.GetField().Single(card => card.ParentCardId == swords[0].Id && card.DefineId == "card_form_ci");
            var otherThrust = owner.GetField().Single(card => card.ParentCardId == swords[1].Id && card.DefineId == "card_form_ci");
            var ctx = new APIContext { Caster = owner, SourceCard = thrust, Scene = scene };
            using var change = lua.GetFunction("ChangeEquipmentStat");
            using var affinity = lua.GetFunction("ChangeAffinity");
            change.Call(ctx, "Self", "Attack", 2, 2);
            change.Call(ctx, "Self", "Defend", -1, 2);
            change.Call(ctx, "Self", "Speed", 0.5, 2);
            affinity.Call(ctx, "Self", "Jin", -1, 1);
            affinity.Call(ctx, "Self", "Mu", 1, 1);
            scene.Start();
            Check(owner.Backpack.Mana.Values.SequenceEqual([ElementType.Mu]), "基础补灵使用唯一有效木亲和");
            using var draw = lua.GetFunction("Draw");
            draw.Call(ctx, "Self", 1);
            Check(owner.Backpack.Mana.Count == 2 && owner.Backpack.Mana.Values.All(element => element.Equals(ElementType.Mu)),
                "主动抽灵使用同一份战斗亲和");
            Check(a.GetAffinity().Jin == 1 && a.GetAffinity().Mu == 0, "亲和修正只影响战斗快照");
            owner.RemoveModifier("Affinity_Mu_Up");
            Check(owner.GetAffinity().Mu == 0, "主动移除亲和 Modifier 同时撤掉属性增量");
            affinity.Call(ctx, "Self", "Mu", 1, 1);

            Check(thrust.GetEquipData().GetStringValue("Element") == "Tu" &&
                otherThrust.GetEquipData().GetStringValue("Element") == "None", "同一招式按绑定实例的配置费用取五行");
            var equip = swordCard.GetEquip();
            Check(equip.Attack == raisedAttack && equip.Defend == weapon.DefendBase - 1 && equip.Speed == weapon.SpeedBase + 0.5f
                && thrust.GetEquipData().GetValue("Attack", 0) == raisedAttack
                && otherThrust.GetEquipData().GetValue("Attack", 0) == weapon.AttackBase
                && swords[0].GetEquip().Attack == weapon.AttackBase, "攻防速只改指定装备实例和它绑定的招式");
            var secondBattle = CombatNpc.CreateCombatNpc(a);
            secondBattle.InitDeck();
            Check(secondBattle.GetField().Single(card => card.Id == swordCard.Id).GetEquip().Attack == weapon.AttackBase,
                "同一个 NPC 的另一场战斗装备数值独立");

            // 亲和验证留下的木灵元也能点亮通用招式，先清掉以隔离拼点样例。
            owner.Backpack.RemoveMana(int.MaxValue, null, scene.Soul);
            var cell = owner.Backpack.EmptyCells()[0];
            owner.Backpack.PlaceMana(cell, ElementType.Jin);
            Check(owner.Backpack.TryAssign(cell, thrust), "为真实剑刺点亮");
            for (int i = 0; i < 10; i++) scene.Tick();
            Check(owner.PendingSlot?.SourceCard == thrust && owner.PendingSlot.ContestValue == raisedAttack,
                "真实剑刺 Lua 用修改后的装备攻击构造拼点");
            Check(owner.PendingSlot.Element.Equals(ElementType.Tu) &&
                new DamageInfo(owner.PendingSlot).ElementType.Equals(ElementType.Tu), "受生终点从装备传入真实剑刺拼点及伤害");
            change.Call(ctx, "Self", "Attack", 1, 1);
            Check(thrust.GetEquipData().GetValue("Attack", 0) == raisedAttack + 1 && owner.PendingSlot.ContestValue == raisedAttack,
                "已构造待发不随装备数值倒改");
            for (int i = 10; i < 50; i++) scene.Tick();
            Check(owner.Backpack.Round == 2 && owner.GetAffinity().Jin == 1 && owner.GetAffinity().Mu == 0
                && thrust.GetEquipData().GetValue("Attack", 0) == raisedAttack, "亲和及装备修正按各自期限到期");
            owner.Backpack.Charge(50);
            scene.Tick();
            Check(owner.Backpack.Round == 3 && thrust.GetEquipData().GetValue("Attack", 0) == weapon.AttackBase, "装备持续两轮后恢复底值");
            a.UnassignCard(swords[0].Id);
            var parentOffField = CombatNpc.CreateCombatNpc(a);
            parentOffField.InitDeck();
            Check(parentOffField.GetField().All(card => card.Id != swords[0].Id) &&
                parentOffField.GetField().Single(card => card.Id == thrust.Id).GetEquipData().GetValue("Attack", 0) == weapon.AttackBase,
                "仍装备但未上场的父卡为招式提供同场快照");
            VerifyBoundDefenseContest(a, b, swords);
            VerifyBoundElements(a, b, swords[1]);
        }
        finally
        {
            scene.Cleanup();
        }
    }

    /// <summary>同一招式读取指定父器费用，真实拼点只携带一个伤害元素。</summary>
    private static void VerifyBoundElements(Npc a, Npc b, Card sword)
    {
        var cases = new (string[] Costs, ElementType Expected)[]
        {
            (["Mu", "Huo", "Jin"], ElementType.Huo),
            (["Mu", "Huo", "Tu", "Jin"], ElementType.Jin),
            (["Mu", "Huo", "Tu", "Jin", "Shui"], ElementType.Tu),
            (["None"], ElementType.None)
        };
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "绑定元素验证"));
        foreach (var (costs, expected) in cases)
        {
            sword.BaseData.ManaCost = ElementType.ToDic(costs.ToDictionary(id => id, _ => 1));
            var scene = new CombatScene();
            scene.Init(7453, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 60);
            try
            {
                scene.PreStart();
                scene.Start();
                var owner = scene.GetCombatants()[a.Id];
                var form = owner.GetField().Single(c => c.ParentCardId == sword.Id && c.DefineId == "card_form_ci");
                void Charge(CombatCard card, ElementType element)
                {
                    var cell = owner.Backpack.EmptyCells()[0];
                    owner.Backpack.PlaceMana(cell, element);
                    Check(owner.Backpack.TryAssign(cell, card), "绑定元素样例真实充灵");
                }
                if (expected.Equals(ElementType.None))
                    Charge(owner.GetField().Single(c => c.Id == sword.Id), ElementType.Huo);
                Charge(form, ElementType.Jin);
                for (int tick = 0; tick < 10; tick++) scene.Tick();
                Check(owner.PendingSlot?.SourceCard == form && owner.PendingSlot.Element.Equals(expected)
                    && new DamageInfo(owner.PendingSlot).ElementType.Equals(expected), "装备到拼点和伤害：" + expected);
            }
            finally { scene.Cleanup(); }
        }
    }

    private static void VerifyBoundDefenseContest(Npc a, Npc b, Card[] swords)
    {
        swords[0].GetEquip().Defend = 2;
        swords[1].GetEquip().Defend = 3;
        foreach (var sword in swords)
        {
            var scene = new CombatScene();
            scene.Init(7452, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 100);
            try
            {
                scene.PreStart();
                scene.Start();
                var owner = scene.GetCombatants()[a.Id];
                var form = owner.GetField().Single(c => c.ParentCardId == sword.Id && c.DefineId == "card_form_ge");
                owner.Backpack.RemoveMana(int.MaxValue, null, scene.Soul);
                var cell = owner.Backpack.EmptyCells()[0];
                owner.Backpack.PlaceMana(cell, ElementType.Jin);
                Check(owner.Backpack.TryAssign(cell, form), "为独立防御样例点亮绑定招式");
                for (int i = 0; i < 20; i++) scene.Tick();
                Check(owner.PendingSlot?.SourceCard == form && owner.PendingSlot.ContestType == ContestType.Block
                    && owner.PendingSlot.FinalValue == sword.GetEquip().Defend, "两张格分别读取各自装备的防御点数");
            }
            finally { scene.Cleanup(); }
        }
    }

    private static void VerifySimultaneousRoundEffects(Lua lua)
    {
        var a = BackpackModelTests.CreateNpc("bp_main_8x3");
        var b = BackpackModelTests.CreateNpc("bp_main_8x3");
        foreach (var npc in new[] { a, b })
            npc.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        var scene = new CombatScene();
        scene.Init(7358, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 100);
        using var fortify = lua.GetFunction("AddFortification");
        var first = Math.Min(a.Id, b.Id);
        var later = Math.Max(a.Id, b.Id);
        var roundStart = new DelegateEventListener((_, _, args) =>
        {
            var ctx = (APIContext)args;
            if (ctx.Caster.Id == first && ctx.GetValue("Round", 0) == 2) fortify.Call(ctx, "Target", 2);
        });
        EventMgr.Instance.RegisterEvent("tca-simultaneous-round", "OnRoundStart", ScopeKey.Global, roundStart);
        try
        {
            scene.PreStart();
            scene.Start();
            for (int i = 0; i < 30; i++) scene.Tick();
            var target = scene.GetCombatants()[later];
            Check(target.Backpack.Round == 2 &&
                target.GetAllModifiers().Single(effect => effect.DefineId == "Fortification").RemainingRounds == 1,
                "同 Tick 换轮时，向稍后发布轮首事件的对手施加 Buff 也使用新轮次");
            for (int i = 30; i < 60; i++) scene.Tick();
            Check(target.GetAllModifiers().All(effect => effect.DefineId != "Fortification"), "目标新轮结束时到期");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnRoundStart", ScopeKey.Global, roundStart);
            scene.Cleanup();
        }
    }

    private static void VerifyArtifactPower(Lua lua)
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        b.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        var cards = new[] { BackpackModelTests.Place(a, "bp_focus", 1, 0), BackpackModelTests.Place(a, "bp_focus", 2, 0) };
        foreach (var card in cards) card.BaseData.Keywords = ["FaBao"];
        cards[0].BaseData.ManaCost = new() { [ElementType.Huo] = 1 };
        using var power = lua.GetFunction("AddArtifactPower");

        (int Fire, int Metal) RunPower(string element, int amount, bool verifyNextRound)
        {
            var scene = new CombatScene();
            scene.Init(7354, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 200);
            try
            {
                scene.PreStart();
                var npc = scene.GetCombatants()[a.Id];
                var ctx = new APIContext { Caster = npc, Scene = scene };
                power.Call(ctx, "Self", element, amount, 0);
                scene.Start();
                var artifacts = cards.Select(card => npc.GetField().Single(current => current.Id == card.Id)).ToArray();
                var first = (artifacts[0].LitCount, artifacts[1].LitCount);
                Check(npc.GetMp() == npc.GetCombatMpMax() && npc.Backpack.Mana.Count == 0
                    && artifacts.All(artifact => artifact.UseCount == 0), "供能不以 MP 付费、不落格且不直接发动");
                if (verifyNextRound)
                {
                    Check(artifacts[0].AssignedElements.SequenceEqual([ElementType.Huo]) && artifacts[1].LitCount == 0,
                        "火供能 3 只填入火法宝缺少的 1 火");
                    npc.Backpack.Charge(50);
                    scene.Tick();
                    Check(npc.Backpack.Round == 2 && artifacts[0].UseCount == 1 && artifacts[0].LitCount == 1
                        && artifacts[1].UseCount == 0, "次轮再次蓄灵，仍由扫描发动");
                }
                return first;
            }
            finally
            {
                scene.Cleanup();
            }
        }

        RunPower("Huo", 3, true);
        var first = RunPower("None", 1, false);
        Check(first == RunPower("None", 1, false) && first.Fire + first.Metal == 1,
            "同种子通用供能重复选择同一个合法候选");
    }
}
