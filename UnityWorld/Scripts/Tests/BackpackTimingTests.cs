using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests
{
    /// <summary>真实时间、保留余量、充能多行与调度边界的确定性回归。</summary>
    public static class BackpackTimingTests
    {
        private static int _assertions;
        private static void Check(bool value, string message)
        {
            _assertions++;
            if (!value) throw new InvalidOperationException("背包时间断言失败：" + message);
        }

        private static Npc NewNpc(string main = "bp_main_6x5")
        {
            var npc = BackpackModelTests.CreateNpc(main);
            npc.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "timing-test"));
            return npc;
        }

        private static CombatScene Scene(Npc a, Npc b)
        {
            var scene = new CombatScene();
            scene.Init(6001, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 1000);
            scene.PreStart(); scene.Start();
            return scene;
        }

        private static void Light(CombatNpc npc, CombatCard card)
        {
            foreach (var cost in card.GetManaCost())
            for (int i = 0; i < cost.Value; i++)
            {
                var cell = npc.Backpack.EmptyCells()[0];
                npc.Backpack.PlaceMana(cell, cost.Key.Equals(ElementType.None) ? ElementType.Jin : cost.Key);
                Check(npc.Backpack.TryAssign(cell, card), "点亮布置成功");
            }
        }

        /// <summary>覆盖设计矩阵 T1-T4 及多行压力场景。</summary>
        public static void Run()
        {
            _assertions = 0;
            ExactClock();
            MultipleRows();
            EffectBoundary();
            SimultaneousRoundEnd();
            FormRoundLimit();
            DefeatStopsCharge();
            Console.WriteLine($"BACKPACK_TIMING_TESTS_OK assertions={_assertions}");
        }

        private static void ExactClock()
        {
            var source = NewNpc();
            var layout = source.GetBackpackSnapshot();
            var cards = source.GetCombatCardSnapshot();
            var fast = new CombatBackpack(layout, cards);
            fast.AddHaste(20);
            for (int i = 0; i < 4; i++) Check(!fast.AdvanceTick(), "双倍速前四 Tick 不满行");
            Check(fast.AdvanceTick() && fast.ScanProgress == 0, "双倍速五 Tick 一行");
            var slow = new CombatBackpack(layout, cards);
            slow.AddSlow(20);
            for (int i = 0; i < 19; i++) Check(!slow.AdvanceTick(), "半速精确累计不截断");
            Check(slow.ScanProgress == 9.5m && slow.AdvanceTick() && slow.SlowTicks == 0, "半速二十 Tick 一行且独立到期");
            slow.AdvanceNaturalTick();
            Check(slow.ScanProgress == 1, "减速到期恢复正常");
            var overlap = new CombatBackpack(layout, cards);
            overlap.AddHaste(2); overlap.AddHaste(3); overlap.AddSlow(8);
            for (int i = 0; i < 5; i++) overlap.AdvanceNaturalTick();
            Check(overlap.ScanProgress == 5 && overlap.HasteTicks == 0 && overlap.SlowTicks == 3, "同类延时，异类独立计时倍率相乘");
            for (int i = 0; i < 3; i++) overlap.AdvanceNaturalTick();
            Check(overlap.ScanProgress == 6.5m && overlap.SlowTicks == 0, "加速先到期后仅半速");
            foreach (var mode in new[] { 0, 1, 2, 3 })
            {
                var pack = new CombatBackpack(layout, cards);
                if ((mode & 1) != 0) pack.AddHaste(12);
                if ((mode & 2) != 0) pack.AddSlow(17);
                pack.Charge(4); pack.Charge(10);
                Check(pack.TryTakeRow() && pack.ScanProgress == 4 && !pack.TryTakeRow(), "各倍率充能十进度均一行并保留4/10");
                Check(pack.HasteTicks == ((mode & 1) != 0 ? 12 : 0) && pack.SlowTicks == ((mode & 2) != 0 ? 17 : 0), "充能不扣真实时间");
            }
        }

        private static void MultipleRows()
        {
            var a = NewNpc(); var b = NewNpc();
            var probes = Enumerable.Range(0, 5).Select(y => BackpackModelTests.Place(a, "bp_focus", 2, y)).ToArray();
            var scene = Scene(a, b);
            var na = scene.GetCombatants()[a.Id]; var nb = scene.GetCombatants()[b.Id];
            var cards = probes.Select(world => na.GetField().Single(c => c.Id == world.Id)).ToArray();
            foreach (var card in cards) Light(na, card);
            // 首行卡首次发动后，右侧格的灵元可为第六次行扫描再次点亮它。
            na.Backpack.PlaceMana(new(3, 0), ElementType.Jin);
            for (int i = 0; i < 4; i++) scene.Tick();
            na.Backpack.Charge(60);
            scene.Tick();
            Check(na.Backpack.Round == 2 && na.Backpack.Row == 1 && na.Backpack.ScanProgress == 5, "同 Tick 跨轮六行并保留余量");
            Check(nb.Backpack.Row == 0 && nb.Backpack.ScanProgress == 5, "充能不推进对手");
            Check(cards.Select(c => c.UseCount).SequenceEqual(new[] { 2, 1, 1, 1, 1 }), "六次扫描逐行实际发动，跨轮再次扫到首行");
            na.Backpack.Charge(10000); scene.Tick();
            Check(na.Backpack.Round == 202 && na.Backpack.Row == 1 && na.Backpack.ScanProgress == 6, "一千合法行压力不静默截断");
            scene.Cleanup();
        }

        private static void EffectBoundary()
        {
            var a = NewNpc(); var b = NewNpc();
            var worldCard = BackpackModelTests.Place(a, "bp_focus", 1, 0);
            var scene = Scene(a, b); var npc = scene.GetCombatants()[a.Id];
            var card = npc.GetField().Single(c => c.Id == worldCard.Id);
            using var lua = new NLua.Lua();
            card.env = (NLua.LuaTable)lua.DoString("return {Apply=function(self,ctx) ctx.Caster.Backpack:AddHaste(1); ctx.Caster.Backpack:Charge(20) end}")[0];
            card.ScanLuaHooks(); Light(npc, card);
            for (int i = 0; i < 10; i++) scene.Tick();
            Check(card.UseCount == 1 && card.ApplyCount == 1 && npc.Backpack.Row == 3, "行内充能完成当前行后再扫新增两行，无重入");
            Check(npc.Backpack.HasteTicks == 1 && npc.Backpack.ScanProgress == 0, "行内一 Tick 加速未被额外扫描扣除");
            scene.Tick();
            Check(npc.Backpack.HasteTicks == 0 && npc.Backpack.ScanProgress == 2, "新加速从下一真实 Tick 生效一次");
            scene.Cleanup();
        }

        private static void SimultaneousRoundEnd()
        {
            var a = NewNpc(); var b = NewNpc();
            var wa = BackpackModelTests.Place(a, "bp_attack", 2, 4);
            var wb = BackpackModelTests.Place(b, "bp_guard", 2, 4);
            var scene = Scene(a, b); var na = scene.GetCombatants()[a.Id]; var nb = scene.GetCombatants()[b.Id];
            var ca = na.GetField().Single(c => c.Id == wa.Id); var cb = nb.GetField().Single(c => c.Id == wb.Id);
            Light(na, ca); Light(nb, cb); var hp = nb.GetHp();
            na.Backpack.Charge(50); nb.Backpack.Charge(50); scene.Tick();
            Check(nb.GetHp() == hp - 2 && ca.ApplyCount == 1 && cb.ApplyCount == 1, "同时多行跨轮先配对一次");
            Check(na.PendingSlot == null && nb.PendingSlot == null && !scene.TraceLog.Any(x => x.Contains("轮末强制")), "同时轮末不重复直击");
            Check(na.Backpack.Round == 2 && nb.Backpack.Round == 2, "双方各自轮末一次");
            scene.Cleanup();
        }

        private static void DefeatStopsCharge()
        {
            var a = NewNpc(); var b = NewNpc();
            b.Stats.SetBase("ShenShi", 1);
            var worldCard = BackpackModelTests.Place(a, "bp_focus", 1, 0);
            var scene = Scene(a, b); var na = scene.GetCombatants()[a.Id]; var nb = scene.GetCombatants()[b.Id];
            var card = na.GetField().Single(c => c.Id == worldCard.Id);
            using var lua = new NLua.Lua();
            card.env = (NLua.LuaTable)lua.DoString("return {Apply=function(self,ctx) ctx.Caster:GetTarget():AddWound('bp_wound_small') end}")[0];
            card.ScanLuaHooks(); Light(na, card);
            na.Backpack.Charge(100); nb.Backpack.Charge(100); scene.Tick();
            Check(scene.IsFinished && !nb.IsActive && na.Backpack.Row == 1 && nb.Backpack.Row == 1, "同批完成后败退，不继续剩余九行充能");
            Check(na.Backpack.ScanProgress == 91 && nb.Backpack.ScanProgress == 91, "败退后未消费剩余进度");
            scene.Cleanup();
        }

        private static void FormRoundLimit()
        {
            var a = NewNpc();
            var b = NewNpc();
            var shortSword = EquipDefineMgr.Instance.Get("short_sword");
            Card PlaceSlash(int row, int spareColumn)
            {
                var weapon = a.GainEquip("card_fabao_jian", shortSword.ID);
                a.EquipFaBao(weapon.Id);
                var forms = a.GetAllCards().Where(c => c.Id != weapon.Id && c.ParentCardId == weapon.Id).ToArray();
                var slash = forms.Single(c => c.DefineId == "card_form_zhan");
                a.PlaceBackpackCardInternal(slash.Id, 1, row);
                a.PlaceBackpackCardInternal(forms.Single(c => c.DefineId == "card_form_jie").Id, spareColumn, 4);
                return slash;
            }
            var first = PlaceSlash(0, 1);
            var second = PlaceSlash(3, 2);
            var block = BackpackModelTests.Place(b, "bp_guard", 1, 1);
            var scene = Scene(a, b);
            var owner = scene.GetCombatants()[a.Id];
            var opponent = scene.GetCombatants()[b.Id];
            var card = owner.GetField().Single(c => c.Id == first.Id);
            var other = owner.GetField().Single(c => c.Id == second.Id);
            void To(int tick)
            {
                while (!scene.IsFinished && scene.CurrentTick < tick) scene.Tick();
            }
            void PlaceAgainOnRow(int row)
            {
                owner.RemoveCombatCard(card);
                owner.DealFieldChange();
                // 占住更早的空格，让同一战斗实例在后面的行重新进包。
                foreach (var cell in owner.Backpack.EmptyCells().Where(c => c.Y < row).ToArray())
                    owner.Backpack.PlaceMana(cell, ElementType.Huo);
                owner.AddCombatCard(card);
                owner.DealFieldChange();
                Check(owner.Backpack.GetCardCells(card.Id).All(c => c.Y == row), "同一招式实例改到稍后的扫描行");
                Light(owner, card);
            }

            Light(owner, card);
            card.Freeze();
            To(10);
            Check(card.UseCount == 0 && card.LitCount == 1 && !card.IsFrozen, "冻结失败后保留本轮次数");
            PlaceAgainOnRow(1);
            Light(opponent, opponent.GetField().Single(c => c.Id == block.Id));
            To(20);
            Check(card.UseCount == 1 && card.ApplyCount == 1 && owner.PendingSlot == null,
                "冻结未消耗次数，包外父装备仍可供招式发动及对拼");
            PlaceAgainOnRow(2);
            To(30);
            Check(card.UseCount == 1 && card.LitCount == 1, "同轮移位后重新满灵仍受上限约束");
            Light(owner, other);
            To(40);
            Check(other.UseCount == 1 && owner.PendingSlot?.SourceCard.Id == second.Id &&
                owner.PendingSlot.ContestValue == shortSword.AttackBase && card.ParentCardId != other.ParentCardId,
                "另一把短剑的同名招式拥有独立次数且取自己的攻击");
            To(80);
            Check(owner.Backpack.Round == 2 && card.UseCount == 2, "本方下一轮恢复一次招式次数");
            scene.Cleanup();
        }
    }
}
