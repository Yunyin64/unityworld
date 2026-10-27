using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests
{
    /// <summary>真实 JSON + 世界管理器的背包模型断言；异常使控制台返回失败。</summary>
    public static class BackpackModelTests
    {
        private static int _assertions;
        private static void Check(bool condition, string message)
        {
            _assertions++;
            if (!condition) throw new InvalidOperationException("背包断言失败：" + message);
        }
        private static void Reject(Action action, string message)
        {
            try { action(); }
            catch (InvalidOperationException) { _assertions++; return; }
            throw new InvalidOperationException("应拒绝：" + message);
        }

        /// <summary>显式绑定主功法；不猜选已有修炼槽，也不为普通 NPC 自动补默认主功法。</summary>
        public static Npc CreateNpc(string mainId = "bp_main_6x5")
        {
            var npc = NpcMgr.Instance.AssembleGenerated(new NpcDefine { ID = "geometry-test", InitStat = new() { ["QiXue"] = 10, ["TiPo"] = 10, ["QiGan"] = 10, ["LingJi"] = 10, ["ShenShi"] = 10, ["WuXing"] = 10, ["JiYuan"] = 10, ["MeiLi"] = 10 } });
            var main = npc.GainCard(mainId);
            GongFaMgr.Instance.InstantiateFromDefine(main.Id, CultivationDefineMgr.Instance.Get(mainId));
            npc.SelectMainGongFa(main.Id);
            return npc;
        }
        public static Card Place(Npc npc, string definition, int x, int y)
        {
            var card = npc.GainCard(definition);
            npc.PlaceBackpackCardInternal(card.Id, x, y);
            return card;
        }
        private static CombatNpc Snapshot(Npc npc)
        {
            var combat = CombatNpc.CreateCombatNpc(npc);
            combat.InitDeck();
            return combat;
        }
        private static CombatCard Find(CombatNpc npc, Card card) => npc.GetField().Single(c => c.Id == card.Id);

        public static void Run()
        {
            _assertions = 0;
            Layouts();
            IsolationAndCarry();
            TargetSelection();
            CombatRegression();
            LuaFixtures();
            Console.WriteLine($"BACKPACK_MODEL_TESTS_OK assertions={_assertions}");
        }

        private static void Layouts()
        {
            var missing = NpcMgr.Instance.AssembleGenerated(new NpcDefine { ID = "geometry-test", InitStat = new() { ["QiXue"] = 10, ["TiPo"] = 10, ["QiGan"] = 10, ["LingJi"] = 10, ["ShenShi"] = 10, ["WuXing"] = 10, ["JiYuan"] = 10, ["MeiLi"] = 10 } });
            Reject(() => CombatNpc.CreateCombatNpc(missing), "没有主功法");
            var a = CreateNpc();
            var b = CreateNpc("bp_main_8x3");
            var da = a.ValidateBackpackLayout();
            var db = b.ValidateBackpackLayout();
            Check(da.Width == 6 && da.Height == 5 && da.GetValidCells().Count == 30, "宽6高5");
            Check(db.Width == 8 && db.Height == 3 && db.GetValidCells().Count == 24, "宽8高3");
            var main = a.GetCardLoadoutSnapshot().MainGongFaCardId.Value;
            Reject(() => a.PlaceBackpackCardInternal(main, 1, 0), "移动主功法");
            Reject(() => a.AssignToReserve(main), "候补主功法");
            Reject(() => a.UnassignCard(main), "取消主功法");
            Reject(() => a.RemoveCard(a.GetAllCards().Single(c => c.Id == main)), "移除主功法");
            var card = Place(a, "bp_charge", 2, 1);
            Check(a.GetFieldSize() == 4, "三格卡占三SP，主卡占一SP");
            Reject(() => a.PlaceBackpackCardInternal(card.Id, 0, 0), "覆盖主功法");
            Reject(() => a.PlaceBackpackCardInternal(card.Id, 5, 4), "越界");
            var other = Place(a, "bp_attack", 1, 1);
            Reject(() => a.PlaceBackpackCardInternal(other.Id, 2, 1), "重叠");
            Check(a.GetCardLoadoutSnapshot().Placements[other.Id][0] == new BackpackCell(1, 1), "失败编辑回滚");
            a.AssignToReserve(other.Id);
            Check(!a.GetCardLoadoutSnapshot().Placements.ContainsKey(other.Id) && a.GetReserveIds().Contains(other.Id), "候补移除占格");
            a.PlaceBackpackCardInternal(other.Id, 4, 4);
            Check(!a.GetReserveIds().Contains(other.Id), "放置移回Field");
            var secondary = a.GainCard("bp_main_8x3");
            GongFaMgr.Instance.InstantiateFromDefine(secondary.Id, CultivationDefineMgr.Instance.Get("bp_main_8x3"));
            a.PlaceBackpackCardInternal(secondary.Id, 5, 4);
            Check(a.ValidateBackpackLayout().GetValidCells().Count == 30, "副功法不扩包");
            var cloned = a.GetCardLoadoutSnapshot();
            cloned.Placements[card.Id].Clear();
            cloned.Field.Clear();
            Check(a.GetCardLoadoutSnapshot().Placements[card.Id].Count == 3 && a.GetFieldIds().Count == 4, "布局深拷贝");
            var duplicate = a.GetCardLoadoutSnapshot();
            duplicate.Field.Add(card.Id);
            Reject(() => NpcBackpackLayout.Validate(duplicate), "重复Field");
            duplicate = a.GetCardLoadoutSnapshot();
            duplicate.Placements[card.Id][1] = duplicate.Placements[card.Id][0];
            Reject(() => NpcBackpackLayout.Validate(duplicate), "重复占格");
            duplicate = a.GetCardLoadoutSnapshot();
            duplicate.Placements[main] = [new(3, 0)];
            Reject(() => NpcBackpackLayout.Validate(duplicate), "篡改主功法固定位置");
            a.SelectMainGongFa(main, 2);
            Check(a.ValidateBackpackLayout().Width == 8 && a.GetCardLoadoutSnapshot().Placements[main][0] == new BackpackCell(1, 0), "等级更新空间与固定格");
            var badDefinition = da.Clone();
            badDefinition.MainCardCells.Add(new(0, 0));
            Reject(() => badDefinition.GetValidCells(), "固定格重复");
            var badShape = CombatCard.CreateFromData(card);
            badShape.BaseData.Shape.RemoveAt(0);
            Reject(() => NpcBackpackLayout.ValidateCard(badShape), "Size与形状不符");
            badShape = CombatCard.CreateFromData(card);
            badShape.BaseData.ManaCost[ElementType.Jin] = 2;
            Reject(() => NpcBackpackLayout.ValidateCard(badShape), "Size与激活数量不符");
            var mismatchedMain = CreateNpc();
            var mainCard = mismatchedMain.GetAllCards().Single(c => c.Id == mismatchedMain.GetCardLoadoutSnapshot().MainGongFaCardId);
            mainCard.BaseData.Size = 2;
            mainCard.BaseData.Shape = [new(0, 0), new(1, 0)];
            Reject(() => mismatchedMain.ValidateBackpackLayout(), "固定主卡占格与Size不符");
            var highSp = CreateNpc("bp_main_8x3");
            highSp.Stats.SetBase("SpMax", 100);
            Reject(() => CombatNpc.CreateCombatNpc(highSp), "SP上限超过空间");
            var sparse = new BackpackLevelDefine { Width = 3, Height = 2, ValidCells = [new(0, 0), new(2, 1)], MainCardCells = [new(0, 0)] };
            Check(sparse.GetValidCells().Count == 2, "非矩形有效格");
        }

        private static void IsolationAndCarry()
        {
            var world = CreateNpc();
            var card = Place(world, "bp_charge", 2, 0);
            var a = Snapshot(world);
            var b = Snapshot(world);
            var ca = Find(a, card);
            var cb = Find(b, card);
            a.Backpack.PlaceMana(new(1, 0), ElementType.Jin);
            Check(a.Backpack.TryAssign(new(1, 0), ca), "单次分配");
            Check(!a.Backpack.TryAssign(new(1, 0), ca), "不重复分配");
            a.Backpack.PlaceMana(new(1, 1), ElementType.Jin);
            a.Backpack.TryAssign(new(1, 1), ca);
            a.Backpack.PlaceMana(new(5, 4), ElementType.Huo);
            for (var tick = 0; tick < 50; tick++)
                if (a.Backpack.AdvanceTick()) a.Backpack.FinishRow();
            Check(a.Backpack.Round == 2 && a.Backpack.Row == 0 && b.Backpack.Round == 1, "扫描时钟独立");
            Check(a.Backpack.AssignedTo(card.Id).Count == 2 && a.Backpack.Mana.ContainsKey(new(5, 4)), "卡上与格上灵元跨轮");
            Check(b.Backpack.AssignedTo(card.Id).Count == 0 && b.Backpack.Mana.Count == 0, "战斗资源隔离");
            Check(!a.Backpack.HasActivationResources(ca), "Size3持2不满足");
            a.Backpack.PlaceMana(new(1, 2), ElementType.Jin);
            a.Backpack.TryAssign(new(1, 2), ca);
            Check(a.Backpack.HasActivationResources(ca) && !a.Backpack.CanAccept(ca, ElementType.Jin), "满3且不溢收");
            Check(a.Backpack.EmptyCells().Count == 25, "空间30减卡4减未分配灵元1");
            Reject(() => a.Backpack.PlaceMana(new(0, 0), ElementType.Jin), "灵元覆盖主卡");
            Reject(() => a.Backpack.PlaceMana(new(5, 4), ElementType.Jin), "灵元覆盖旧灵元");
            Check(!a.Backpack.IsCardCompleteAt(card.Id, new(2, 1)) && a.Backpack.IsCardCompleteAt(card.Id, new(2, 2)), "最后占格决定扫描完整");
            ca.BaseData.Keywords.Add("test-only");
            ca.BaseData.ManaCost[ElementType.Jin] = 1;
            ca.BaseData.Shape.Clear();
            Check(!card.HasKeyword("test-only") && !cb.HasKeyword("test-only") && card.GetManaCost()[ElementType.Jin] == 3 && cb.BaseData.Shape.Count == 3, "CardBaseData私有");
            var beforeInit = CombatNpc.CreateCombatNpc(world);
            card.BaseData.Keywords.Add("world-later");
            world.AssignToReserve(card.Id);
            beforeInit.InitDeck();
            Check(Find(beforeInit, card).BaseData.Shape.Count == 3 && !Find(beforeInit, card).HasKeyword("world-later"), "创建时快照不受后续世界变更影响");
            var shortClock = new CombatBackpack(CreateNpc("bp_main_8x3").GetCardLoadoutSnapshot());
            for (var tick = 0; tick < 30; tick++) if (shortClock.AdvanceTick()) shortClock.FinishRow();
            Check(shortClock.Round == 2, "3行30Tick");
        }

        private static void TargetSelection()
        {
            var world = CreateNpc();
            var left = Place(world, "bp_attack", 1, 1);
            var right = Place(world, "bp_attack", 3, 1);
            var lower = Place(world, "bp_attack", 2, 2);
            var combat = Snapshot(world);
            var bag = combat.Backpack;
            var selector = new BackpackManaTargetSelector();
            var source = new BackpackCell(2, 1);
            bag.PlaceMana(source, ElementType.Jin);
            Check(selector.AssignAt(bag, source, combat.GetField()).Id == lower.Id, "等距先下方");
            bag.PlaceMana(source, ElementType.Jin);
            Check(selector.AssignAt(bag, source, combat.GetField()).Id == left.Id, "已满跳过、等距左方");
            bag.PlaceMana(new(0, 1), ElementType.Jin);
            Check(selector.AssignAt(bag, new(0, 1), combat.GetField()).Id == right.Id, "最近已满时找较远需要卡");
            bag.PlaceMana(source, ElementType.Jin);
            Check(selector.AssignAt(bag, source, combat.GetField()) == null && bag.Mana.ContainsKey(source), "无候选原地保留");
            Check(bag.AssignedTo(right.Id).Count == 1, "重复扫描不复制资源");
            var other = Snapshot(world);
            other.Backpack.PlaceMana(source, ElementType.Huo);
            Check(selector.AssignAt(other.Backpack, source, other.GetField()) == null, "缺灵但元素不匹配不接收");
            var injected = new BackpackManaTargetSelector((_, cells) => cells[0].X == 3 ? 0 : 10);
            Check(injected.Select(other.Backpack, source, ElementType.Jin, other.GetField()).Id == right.Id, "距离策略可注入");
        }

        private static CombatScene Scene(int ticks = 3)
        {
            var scene = new CombatScene();
            scene.Init(12345, [(CreateNpc(), CombatTeam.TeamA), (CreateNpc("bp_main_8x3"), CombatTeam.TeamB)], ticks);
            return scene;
        }

        private static void LuaFixtures()
        {
            foreach (var definition in new[] { "bp_attack", "bp_charge", "bp_guard", "bp_focus" })
            {
                var world = CreateNpc();
                world.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "scan-test"));
                var worldCard = Place(world, definition, 2, 0);
                var scene = new CombatScene();
                scene.Init(12345, [(world, CombatTeam.TeamA), (CreateNpc("bp_main_8x3"), CombatTeam.TeamB)], 100);
                scene.PreStart(); scene.Start();
                var npc = scene.GetCombatants()[world.Id];
                var card = Find(npc, worldCard);
                for (int i = 0; i < card.GetSize(); i++)
                {
                    var cell = npc.Backpack.EmptyCells()[0];
                    npc.Backpack.PlaceMana(cell, ElementType.Jin);
                    npc.Backpack.TryAssign(cell, card);
                }
                for (int i = 0; i < (definition == "bp_charge" ? 30 : 10); i++) scene.Tick();
                Check(card.UseCount == 1, "Lua卡由扫描使用一次");
                if (definition == "bp_focus") Check(npc.ShieldValue == 2 && card.ApplyCount == 1, "无拼点Lua效果一次");
                else
                {
                    Check(npc.PendingSlot?.SourceCard == card && card.ApplyCount == 0, "拼点先单槽等待，不提前Apply");
                    var expected = definition == "bp_charge" ? 12 : definition == "bp_guard" ? 4 : 6;
                    Check(npc.PendingSlot.ContestValue == expected, "拼点Lua数值");
                    while (scene.CurrentTick < 50) scene.Tick();
                    Check(npc.PendingSlot == null && card.ApplyCount == 1, "本方轮末出槽后Apply一次");
                }
                scene.Cleanup();
            }
        }

        private static void CombatRegression()
        {
            var limited = Scene(3);
            var result = limited.Run();
            Check(result.GetValue<CombatEndReason>("EndReason") == CombatEndReason.TurnLimitReached && result.GetValue<int>("TotalTicks") == 3, "Run返回真实结果，严格Tick上限");
            Check(result.GetValue<CombatTeam>("WinnerTeam") == CombatTeam.None && result.Get<List<CombatantResult>>("Combatants").Count == 2, "上限平局记录双方结果");
            var victory = Scene();
            victory.GetCombatants().Values.Last().Status = CombatantStatus.Defeated;
            result = victory.Run();
            Check(result.GetValue<CombatTeam>("WinnerTeam") == CombatTeam.TeamA && result.GetValue<CombatEndReason>("EndReason") == CombatEndReason.AllDefeated, "单方胜利");
            var both = Scene();
            foreach (var npc in both.GetCombatants().Values) npc.Status = CombatantStatus.Defeated;
            result = both.Run();
            Check(result.GetValue<CombatTeam>("WinnerTeam") == CombatTeam.None && result.Get<List<CombatantResult>>("Combatants").All(c => c.IsDefeated), "双方同时败退安全结束");

            foreach (var sample in new[]
            {
                (ContestType.Zhan, 6, ContestType.Zhan, 4, 6),
                (ContestType.Zhan, 6, ContestType.Ci, 4, 2),
                (ContestType.SheJi, 6, ContestType.SheJi, 4, 2),
                (ContestType.Zhan, 4, ContestType.Zhan, 4, 0),
                (ContestType.Block, 6, ContestType.Zhan, 4, 0)
            })
            {
                var scene = Scene();
                scene.PreStart(); scene.Start();
                var a = scene.GetCombatants().Values.First();
                var b = scene.GetCombatants().Values.Last();
                var hpA = a.GetHp(); var hpB = b.GetHp();
                a.ResolveContest(new ContestData { SourceCard = a.GetField()[0], ContestType = sample.Item1, ContestValue = sample.Item2 },
                    new ContestData { SourceCard = b.GetField()[0], ContestType = sample.Item3, ContestValue = sample.Item4 });
                a.DealDamage(); b.DealDamage();
                Check(hpA == a.GetHp() && hpB - b.GetHp() == sample.Item5, $"拼点回归 {sample.Item1}/{sample.Item3}");
                scene.Cleanup();
            }
            var shieldScene = Scene();
            shieldScene.PreStart(); shieldScene.Start();
            var attacker = shieldScene.GetCombatants().Values.First();
            var defender = shieldScene.GetCombatants().Values.Last();
            var hp = defender.GetHp();
            defender.ChangeShield(3);
            attacker.Straight(new ContestData { SourceCard = attacker.GetField()[0], ContestType = ContestType.Zhan, ContestValue = 6 });
            defender.DealDamage();
            Check(hp - defender.GetHp() == 3 && defender.ShieldValue == 0, "直击全额6，盾吸收3");
            hp = defender.GetHp();
            attacker.Straight(new ContestData { SourceCard = attacker.GetField()[0], ContestType = ContestType.Block, ContestValue = 6 });
            defender.DealDamage();
            Check(hp == defender.GetHp(), "防御直击不伤害");
            shieldScene.Cleanup();
        }
    }
}
