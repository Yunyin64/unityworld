using UnityWorld.Core;
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
                npc.Backpack.PlaceMana(cell, cost.Key);
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
            DerivedPendingWaitsForNextBoundary();
            DefeatStopsCharge();
            Console.WriteLine($"BACKPACK_TIMING_TESTS_OK assertions={_assertions}");
        }

        private static void ExactClock()
        {
            var loadout = NewNpc().GetCardLoadoutSnapshot();
            var fast = new CombatBackpack(loadout);
            fast.AddHaste(20);
            for (int i = 0; i < 4; i++) Check(!fast.AdvanceTick(), "双倍速前四 Tick 不满行");
            Check(fast.AdvanceTick() && fast.ScanProgress == 0, "双倍速五 Tick 一行");
            var slow = new CombatBackpack(loadout);
            slow.AddSlow(20);
            for (int i = 0; i < 19; i++) Check(!slow.AdvanceTick(), "半速精确累计不截断");
            Check(slow.ScanProgress == 9.5m && slow.AdvanceTick() && slow.SlowTicks == 0, "半速二十 Tick 一行且独立到期");
            slow.AdvanceNaturalTick();
            Check(slow.ScanProgress == 1, "减速到期恢复正常");
            var overlap = new CombatBackpack(loadout);
            overlap.AddHaste(2); overlap.AddHaste(3); overlap.AddSlow(8);
            for (int i = 0; i < 5; i++) overlap.AdvanceNaturalTick();
            Check(overlap.ScanProgress == 5 && overlap.HasteTicks == 0 && overlap.SlowTicks == 3, "同类延时，异类独立计时倍率相乘");
            for (int i = 0; i < 3; i++) overlap.AdvanceNaturalTick();
            Check(overlap.ScanProgress == 6.5m && overlap.SlowTicks == 0, "加速先到期后仅半速");
            foreach (var mode in new[] { 0, 1, 2, 3 })
            {
                var pack = new CombatBackpack(loadout);
                if ((mode & 1) != 0) pack.AddHaste(12);
                if ((mode & 2) != 0) pack.AddSlow(17);
                pack.Charge(4); pack.Charge(10);
                Check(pack.TryTakeRow() && pack.ScanProgress == 4 && !pack.TryTakeRow(), "各倍率充能十进度均一行并保留4/10");
                Check(pack.HasteTicks == ((mode & 1) != 0 ? 12 : 0) && pack.SlowTicks == ((mode & 2) != 0 ? 17 : 0), "充能不扣真实时间");
            }
        }

        private static void MultipleRows()
        {
            var a = NewNpc(); var b = NewNpc(); var scene = Scene(a, b);
            var na = scene.GetCombatants()[a.Id]; var nb = scene.GetCombatants()[b.Id];
            for (int i = 0; i < 4; i++) scene.Tick();
            na.Backpack.Charge(60);
            scene.Tick();
            Check(na.Backpack.Round == 2 && na.Backpack.Row == 1 && na.Backpack.ScanProgress == 5, "同 Tick 跨轮六行并保留余量");
            Check(nb.Backpack.Row == 0 && nb.Backpack.ScanProgress == 5, "充能不推进对手");
            Check(scene.TraceLog.Count(x => x.Contains($"Npc={a.Id} ") && x.Contains("扫描行")) == 6, "每个完成行均真实扫描");
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

        private static void DerivedPendingWaitsForNextBoundary()
        {
            var a = NewNpc(); var b = NewNpc();
            var firstA = BackpackModelTests.Place(a, "bp_attack", 1, 0);
            var firstB = BackpackModelTests.Place(b, "bp_guard", 1, 0);
            var nextA = BackpackModelTests.Place(a, "bp_attack", 2, 1);
            var nextB = BackpackModelTests.Place(b, "bp_guard", 2, 1);
            foreach (var source in new[] { nextA, nextB })
            {
                source.BaseData.ScanActivatable = false;
                source.BaseData.Keywords.AddRange(["FaBao", "BoundaryRefillTest"]);
            }
            using var lua = new NLua.Lua();
            var keyword = (NLua.LuaTable)lua.DoString("return {OnApply=function(card,ctx) if ctx.SourceCard ~= card then card:TryUseFromEvent(ctx) end end}")[0];
            LuaMgr.Instance.RegisterKeyword("BoundaryRefillTest", keyword);
            var scene = Scene(a, b); var na = scene.GetCombatants()[a.Id]; var nb = scene.GetCombatants()[b.Id];
            var ca = na.GetField().Single(c => c.Id == nextA.Id); var cb = nb.GetField().Single(c => c.Id == nextB.Id);
            foreach (var npc in new[] { na, nb }) foreach (var card in npc.GetField().Where(c => c.BaseData.HasActiveAbility)) Light(npc, card);
            for (int i = 0; i < 10; i++) scene.Tick();
            Check(ca.UseCount == 1 && cb.UseCount == 1 && ca.ApplyCount == 0 && cb.ApplyCount == 0,
                "对拼Apply事件产生的双方新待发不在同一行边界再次对拼");
            Check(na.PendingSlot?.SourceCard == ca && nb.PendingSlot?.SourceCard == cb, "新槽保留至后续行边界");
            for (int i = 0; i < 10; i++) scene.Tick();
            Check(ca.ApplyCount == 1 && cb.ApplyCount == 1 && na.PendingSlot == null && nb.PendingSlot == null,
                "后续行边界恰好结算派生待发一次");
            scene.Cleanup();
        }
    }
}
