using UnityWorld.Core;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;
using UnityWorld.Game.Data;

namespace UnityWorld.Tests
{
    /// <summary>逐 Tick 驱动真实场景、JSON 和 Lua 的行为断言。</summary>
    public static class BackpackCombatTests
    {
        private static int _count;
        private static void Check(bool condition, string message)
        {
            _count++;
            if (!condition) throw new InvalidOperationException("扫描战斗断言失败：" + message);
        }
        private static Npc Npc(string main = "bp_main_6x5", int draw = 0)
        {
            var npc = BackpackModelTests.CreateNpc(main);
            npc.Stats.AddModifier("ManaDrawCost", StatModifier.Override(draw, "scan-test"));
            npc.Stats.SetBase("AffinityJin", 100);
            foreach (var name in new[] { "Mu", "Shui", "Huo", "Tu" }) npc.Stats.SetBase("Affinity" + name, 0);
            return npc;
        }
        private static Card Place(Npc npc, string def, int x, int y) => BackpackModelTests.Place(npc, def, x, y);
        private static CombatScene Scene(Npc a, Npc b, int max = 200, bool reverse = false)
        {
            var scene = new CombatScene();
            var entries = new[] { (a, CombatTeam.TeamA), (b, CombatTeam.TeamB) };
            scene.Init(4567, reverse ? entries.Reverse() : entries, max);
            scene.PreStart(); scene.Start();
            return scene;
        }
        private static void To(CombatScene scene, int tick)
        {
            while (!scene.IsFinished && scene.CurrentTick < tick) scene.Tick();
        }
        private static CombatCard Card(CombatScene scene, Npc owner, Card source) => scene.GetCombatants()[owner.Id].GetField().Single(c => c.Id == source.Id);
        private static void Light(CombatNpc npc, CombatCard card, int count = -1)
        {
            if (count < 0) count = card.GetSize();
            for (int i = 0; i < count; i++)
            {
                var cell = npc.Backpack.EmptyCells()[0];
                npc.Backpack.PlaceMana(cell, ElementType.Jin);
                Check(npc.Backpack.TryAssign(cell, card), "布置点亮状态");
            }
        }

        public static void Run()
        {
            _count = 0;
            PassiveLifecycle();
            RowOrder();
            DrawAndCarry();
            SlotsAndTimeline();
            Wounds();
            FullBattles();
            Console.WriteLine($"BACKPACK_COMBAT_TESTS_OK assertions={_count}");
        }

        private static void PassiveLifecycle()
        {
            var a = Npc(); var b = Npc("bp_main_8x3"); var scene = Scene(a, b);
            var card = scene.GetCombatants()[a.Id].GetField()[0];
            using var lua = new NLua.Lua();
            card.env = (NLua.LuaTable)lua.DoString("return {OnPassiveTick=function(self,ctx) self.ticks=(self.ticks or 0)+1 end, Tick=function(self,ctx) self.legacy=true end}")[0];
            card.ScanLuaHooks();
            var keyword = (NLua.LuaTable)lua.DoString("return {OnTick=function(card,ctx) card.env.keywordTicks=(card.env.keywordTicks or 0)+1 end}")[0];
            LuaMgr.Instance.RegisterKeyword("BackpackPassiveTest", keyword);
            card.BaseData.Keywords.Add("BackpackPassiveTest");
            To(scene, 3);
            Check(Convert.ToInt32(card.env["ticks"]) == 3 && Convert.ToInt32(card.env["keywordTicks"]) == 3, "被动卡仅执行自身OnPassiveTick和关键词OnTick");
            Check(card.env["legacy"] == null && card.CheckPhase(CombatCardPhase.Passive) && card.Ticks["CD"] == 0 && card.UseCount == 0, "被动卡不走旧Tick和主动出手流程");
            scene.Cleanup();
        }

        private static void RowOrder()
        {
            // A/B 缺灵，C 满：前面缺灵不能占机会。
            var a = Npc(); var b = Npc("bp_main_8x3");
            var ca = Place(a, "bp_focus", 1, 0); var cb = Place(a, "bp_focus", 2, 0); var cc = Place(a, "bp_focus", 3, 0);
            var scene = Scene(a, b); var npc = scene.GetCombatants()[a.Id];
            Light(npc, Card(scene, a, cc));
            To(scene, 9); Check(npc.ShieldValue == 0, "不足10Tick不扫描");
            To(scene, 10);
            Check(Card(scene, a, ca).UseCount == 0 && Card(scene, a, cb).UseCount == 0 && Card(scene, a, cc).UseCount == 1, "连续缺灵保留机会给右侧");
            Check(npc.ShieldValue == 2, "无拼点一次Apply");
            scene.Cleanup();

            // A 缺灵、B/C 满：B 使用，C 仍保持点亮。
            scene = Scene(a, b); npc = scene.GetCombatants()[a.Id];
            Light(npc, Card(scene, a, cb)); Light(npc, Card(scene, a, cc));
            To(scene, 10);
            Check(Card(scene, a, cb).UseCount == 1 && Card(scene, a, cc).UseCount == 0 && npc.Backpack.LitCount(cc.Id) == 1, "左缺灵、右先满者使用，后满者保留");
            scene.Cleanup();

            // 左灵元先点亮右卡；本行机会用完仍继续扫描右灵元。
            a = Npc(); b = Npc("bp_main_8x3"); ca = Place(a, "bp_focus", 2, 0); cb = Place(a, "bp_focus", 4, 0);
            scene = Scene(a, b); npc = scene.GetCombatants()[a.Id];
            npc.Backpack.PlaceMana(new(1, 0), ElementType.Jin);
            npc.Backpack.PlaceMana(new(5, 0), ElementType.Jin);
            To(scene, 10);
            Check(Card(scene, a, ca).UseCount == 1 && Card(scene, a, cb).UseCount == 0, "左灵元先供右卡");
            Check(npc.Backpack.LitCount(cb.Id) == 1 && npc.Backpack.Mana.Count == 0, "机会用完继续分配右灵元");
            To(scene, 50); Check(Card(scene, a, cb).UseCount == 0, "迟到补满本轮不追补");
            To(scene, 60); Check(Card(scene, a, cb).UseCount == 1, "下一轮再次扫完整才使用");
            scene.Cleanup();

            // 跨行卡最后一格才可用；旧关键词/Ready/CD 不能提前发动。
            a = Npc(); b = Npc("bp_main_8x3"); ca = Place(a, "bp_charge", 2, 0);
            ca.BaseData.Keywords.Add("FaShu"); ca.BaseData.Cooldown = 0;
            scene = Scene(a, b); npc = scene.GetCombatants()[a.Id]; var active = Card(scene, a, ca);
            Light(npc, active); active.SetReady(true);
            To(scene, 29); Check(active.UseCount == 0 && npc.PendingSlot == null && active.Ticks["CD"] == 0, "旧CD和Ready无出手机会，等待最后一格");
            To(scene, 30); Check(active.UseCount == 1 && npc.PendingSlot?.SourceCard == active, "多行卡最后格入槽");
            To(scene, 50); Check(active.ApplyCount == 1, "旧关键词不覆盖入槽阶段，不重复Apply");
            active.Apply(); Check(active.ApplyCount == 1, "结算后重复Apply无效果");
            scene.Cleanup();

            // 同行三格卡：夹在首格与末格之间的灵元必须先参与点亮。
            a = Npc(); b = Npc("bp_main_8x3");
            var horizontal = a.GainCard("bp_charge");
            horizontal.BaseData.Shape = [new(0, 0), new(2, 0), new(3, 0)];
            a.PlaceBackpackCard(horizontal.Id, 1, 0);
            scene = Scene(a, b); npc = scene.GetCombatants()[a.Id]; active = Card(scene, a, horizontal);
            Light(npc, active, 2); npc.Backpack.PlaceMana(new(2, 0), ElementType.Jin);
            To(scene, 10);
            Check(active.UseCount == 1 && npc.PendingSlot?.SourceCard == active, "同一行多格卡只在末格判断，先处理中间灵元");
            Check(!npc.Backpack.CanAccept(active, ElementType.Jin), "待发期间不接收下一次点亮资源");
            scene.Cleanup();
        }

        private static void DrawAndCarry()
        {
            var a = Npc(); var b = Npc("bp_main_8x3"); var scene = Scene(a, b); var npc = scene.GetCombatants()[a.Id];
            var empty = npc.Backpack.EmptyCells();
            foreach (var cell in empty.Skip(2)) npc.Backpack.PlaceMana(cell, ElementType.Huo);
            var mp = npc.GetMp(); npc.DrawMana(5);
            Check(npc.GetMp() == mp - 2 && npc.Backpack.EmptyCells().Count == 0 && npc.Backpack.Mana.Values.Count(e => e.Equals(ElementType.Jin)) == 2, "计划5只有2格生成2且扣2MP");
            mp = npc.GetMp(); npc.DrawMana(5); Check(npc.GetMp() == mp, "满背包0生成0成本");
            scene.Cleanup();
            a = Npc(); b = Npc("bp_main_8x3"); scene = Scene(a, b); npc = scene.GetCombatants()[a.Id];
            npc.ReduceMP(npc.GetMp() - 1); npc.DrawMana(5);
            Check(npc.GetMp() == 0 && npc.Backpack.Mana.Count == 1, "MP不足只生成可支付数量"); scene.Cleanup();

            a = Npc(draw: 5); b = Npc("bp_main_8x3", 5); scene = Scene(a, b);
            npc = scene.GetCombatants()[a.Id]; var other = scene.GetCombatants()[b.Id];
            Check(npc.Backpack.Mana.Count == 5 && other.Backpack.Mana.Count == 5, "开场抽取落格");
            To(scene, 20); Check(npc.Backpack.Mana.Count == 5 && other.Backpack.Mana.Count == 5, "没有独立抽取CD");
            To(scene, 30); Check(npc.Backpack.Mana.Count == 5 && other.Backpack.Mana.Count == 10, "短轮只补自己");
            To(scene, 50); Check(npc.Backpack.Mana.Count == 10 && other.Backpack.Mana.Count == 10, "长轮只补自己");
            var trace = scene.TraceLog.ToArray(); scene.Cleanup();
            var replay = Scene(a, b, reverse: true); To(replay, 50);
            Check(trace.SequenceEqual(replay.TraceLog), "相同种子布局可重放且交换注册顺序结果不变"); replay.Cleanup();

            a = Npc(); b = Npc("bp_main_8x3"); var c = Place(a, "bp_charge", 2, 0); scene = Scene(a, b); npc = scene.GetCombatants()[a.Id];
            var card = Card(scene, a, c); Light(npc, card, 2);
            npc.Backpack.PlaceMana(new(5, 4), ElementType.Huo);
            To(scene, 50); Check(npc.Backpack.LitCount(c.Id) == 2 && npc.Backpack.Mana.ContainsKey(new(5, 4)) && card.UseCount == 0, "2/3与无目标灵元跨轮保留");
            Light(npc, card, 1); To(scene, 80); Check(card.UseCount == 1 && npc.Backpack.LitCount(c.Id) == 0, "补至3/3后再次完整扫描使用，开始下次点亮"); scene.Cleanup();
        }

        private static void SlotsAndTimeline()
        {
            var a = Npc("bp_main_8x3"); var b = Npc();
            var ac = Place(a, "bp_attack", 2, 0); var bc = Place(b, "bp_guard", 2, 0);
            var scene = Scene(a, b); var na = scene.GetCombatants()[a.Id]; var nb = scene.GetCombatants()[b.Id];
            Light(na, Card(scene, a, ac)); Light(nb, Card(scene, b, bc)); var hp = nb.GetHp();
            To(scene, 10); Check(na.PendingSlot == null && nb.PendingSlot == null && nb.GetHp() == hp - 2, "每行切换双方有数据即拼，不等轮末");
            Check(Card(scene, a, ac).ApplyCount == 1 && Card(scene, b, bc).ApplyCount == 1, "配对双方各Apply一次"); scene.Cleanup();

            // A轮末槽空不能令B直击；B等自己的50Tick。
            scene = Scene(a, b); na = scene.GetCombatants()[a.Id]; nb = scene.GetCombatants()[b.Id];
            Light(nb, Card(scene, b, bc)); To(scene, 30);
            Check(nb.PendingSlot != null && nb.Backpack.Round == 1 && nb.Backpack.Row == 3 && na.Backpack.Round == 2, "A空槽轮末不清B、不重置B");
            To(scene, 50); Check(nb.PendingSlot == null && Card(scene, b, bc).ApplyCount == 1, "B自己的轮末强制唯一待发"); scene.Cleanup();

            // 单槽占用时继续扫：下一张拼点卡等，无拼点卡仍可使用。
            a = Npc(); b = Npc("bp_main_8x3"); ac = Place(a, "bp_attack", 2, 0); var waiting = Place(a, "bp_attack", 2, 1); var effect = Place(a, "bp_focus", 4, 1);
            scene = Scene(a, b); na = scene.GetCombatants()[a.Id]; nb = scene.GetCombatants()[b.Id];
            Light(na, Card(scene, a, ac)); Light(na, Card(scene, a, waiting)); Light(na, Card(scene, a, effect)); hp = nb.GetHp();
            To(scene, 20);
            Check(na.PendingSlot?.SourceCard.Id == ac.Id && Card(scene, a, waiting).UseCount == 0 && na.Backpack.LitCount(waiting.Id) == 1, "单槽等待不覆盖、不消失");
            Check(Card(scene, a, effect).UseCount == 1 && na.ShieldValue == 2 && nb.GetHp() == hp, "待发占位不占本行机会，无轮中直击");
            To(scene, 50); Check(nb.GetHp() == hp - 6 && na.PendingSlot == null, "本方轮末唯一攻击直击");
            To(scene, 70); Check(na.PendingSlot?.SourceCard.Id == waiting.Id, "已等待点亮卡下轮扫完整才入槽"); scene.Cleanup();

            a = Npc("bp_main_8x3"); b = Npc(); scene = Scene(a, b, 151); na = scene.GetCombatants()[a.Id]; nb = scene.GetCombatants()[b.Id];
            foreach (var tick in new[] { 10, 20, 30, 40, 50, 60, 150 })
            {
                To(scene, tick);
                Check(na.Backpack.Round == tick / 30 + 1 && na.Backpack.Row == tick / 10 % 3, $"短轮时间线{tick}");
                Check(nb.Backpack.Round == tick / 50 + 1 && nb.Backpack.Row == tick / 10 % 5, $"长轮时间线{tick}");
            }
            Check(scene.TraceLog.Count(l => l.Contains("本方新轮")) == 8, "150Tick双方各自轮末不重复"); scene.Cleanup();

            // 150Tick同时扫到末行攻击：先配对，不再分别直击。
            a = Npc("bp_main_8x3"); b = Npc();
            ac = Place(a, "bp_attack", 2, 2); bc = Place(b, "bp_guard", 2, 4);
            scene = Scene(a, b, 151); na = scene.GetCombatants()[a.Id]; nb = scene.GetCombatants()[b.Id];
            To(scene, 149); Light(na, Card(scene, a, ac)); Light(nb, Card(scene, b, bc)); hp = nb.GetHp();
            To(scene, 150);
            Check(nb.GetHp() == hp - 2 && na.PendingSlot == null && nb.PendingSlot == null, "同Tick双轮末先配对一次而非两次直击");
            Check(Card(scene, a, ac).ApplyCount == 1 && Card(scene, b, bc).ApplyCount == 1 && !scene.TraceLog.Any(l => l.StartsWith("Tick=150 ") && l.Contains("轮末强制")), "末行已配对数据不重复Apply或强制结算");
            scene.Cleanup();
        }

        private static void Wounds()
        {
            var a = Npc(); var b = Npc("bp_main_8x3"); var scene = Scene(a, b); var npc = scene.GetCombatants()[a.Id];
            foreach (var cell in npc.Backpack.EmptyCells()) npc.Backpack.PlaceMana(cell, ElementType.Jin);
            var before = npc.Backpack.Mana.Count; var wound = npc.AddWound("bp_wound_small"); npc.DealFieldChange();
            Check(npc.Backpack.Mana.Count == before - 1 && npc.GetSp() == 2 && npc.Backpack.Placements[wound.Id].Count == 1, "满背包伤口替代一个灵元计SP");
            Check(wound.env != null && wound.Ticks.ContainsKey("Main"), "新伤口初始化Lua与Start");
            var wide = npc.AddWound("bp_wound_wide"); npc.DealFieldChange(); var cells = npc.Backpack.Placements[wide.Id];
            Check(cells.Count == 2 && cells[0].Y == cells[1].Y && cells[1].X == cells[0].X + 1 && npc.Backpack.Mana.Count == before - 3, "多格伤口保持完整形状");
            Check(cells.All(c => !npc.Backpack.Mana.ContainsKey(c)), "被替代灵元不再存在"); scene.Cleanup();

            a = Npc(); b = Npc("bp_main_8x3"); scene = Scene(a, b); npc = scene.GetCombatants()[a.Id];
            npc.Backpack.PlaceMana(new(1, 0), ElementType.Jin);
            wide = npc.AddWound("bp_wound_wide"); npc.DealFieldChange();
            Check(npc.Backpack.Placements[wide.Id].SequenceEqual(new BackpackCell[] { new(2, 0), new(3, 0) }) && npc.Backpack.Mana.ContainsKey(new(1, 0)), "优先完整纯空位，保留更靠左的灵元"); scene.Cleanup();

            a = Npc(); b = Npc("bp_main_8x3"); scene = Scene(a, b); npc = scene.GetCombatants()[a.Id];
            foreach (var cell in npc.Backpack.EmptyCells().Where(c => c != new BackpackCell(1, 0))) npc.Backpack.PlaceMana(cell, ElementType.Jin);
            before = npc.Backpack.Mana.Count; wide = npc.AddWound("bp_wound_wide"); npc.DealFieldChange();
            Check(npc.Backpack.Placements[wide.Id].SequenceEqual(new BackpackCell[] { new(1, 0), new(2, 0) }) && npc.Backpack.Mana.Count == before - 1, "一空格加一灵元可放完整两格伤口，仅替代一个灵元"); scene.Cleanup();

            // 人为构造违反设计保证的碎片化数据：明确拒绝，不能伪装成拆格成功。
            a = Npc();
            for (int y = 0; y < 5; y++) for (int x = 0; x < 6; x++)
                if (x % 2 == 1) Place(a, "bp_focus", x, y);
            var fragmented = new CombatBackpack(a.GetCardLoadoutSnapshot());
            var whole = CardMgr.Instance.InstantiateFromDefine("bp_wound_wide");
            Check(fragmented.EmptyCells().Count >= 2 && fragmented.FindPlacement(whole, true) == null, "格数够但形状不合仍判定无完整位置");
            var invalidRejected = false;
            try { fragmented.Insert(whole, true); } catch (InvalidOperationException) { invalidRejected = true; }
            Check(invalidRejected && !fragmented.Placements.ContainsKey(whole.Id), "违背设计保证报错，原子不拆格");

            a = Npc(); b = Npc("bp_main_8x3"); scene = Scene(a, b); npc = scene.GetCombatants()[a.Id]; var attacker = scene.GetCombatants()[b.Id];
            npc.ApplyDamage(new DamageInfo(attacker, attacker.GetField()[0], npc, npc.GetHp())); npc.DealFieldChange(); npc.CheckDefeated();
            Check(npc.IsActive && npc.GetHp() == (int)(npc.GetHpMax() * npc.GetStat("HpZeroRecoverRate")) && npc.GetSp() > 1, "HP归零加伤口恢复，不直接判负");
            while (npc.GetSp() <= npc.GetSpMax()) { npc.AddWound("bp_wound_small"); npc.DealFieldChange(); }
            npc.CheckDefeated(); Check(!npc.IsActive && npc.Backpack.EmptyCells().Count > 0, "尚有空间但SP超限败退");
            var row = npc.Backpack.Row; var round = npc.Backpack.Round; To(scene, 20);
            Check(npc.Backpack.Row == row && npc.Backpack.Round == round, "败退者停止扫描"); scene.Cleanup();
        }

        private static void FullBattles()
        {
            foreach (var main in new[] { "bp_main_6x5", "bp_main_8x3" })
            {
                var a = Npc(main, 5); var b = Npc(main == "bp_main_6x5" ? "bp_main_8x3" : "bp_main_6x5", 5);
                b.Stats.SetBase("ShenShi", 1);
                var card = Place(a, "bp_attack", 5, 0);
                var keywords = card.GetKeywords().ToArray(); var before = b.GetCardLoadoutSnapshot();
                var scene = new CombatScene(); scene.Init(8765, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 1000);
                var result = scene.Run();
                Check(result.GetValue<CombatTeam>("WinnerTeam") == CombatTeam.TeamA && result.GetValue<CombatEndReason>("EndReason") == CombatEndReason.AllDefeated, "完整战斗SP败退 " + main);
                Check(result.Get<List<CombatantResult>>("Combatants").Single(c => c.NpcId == b.Id).IsDefeated, "结果正确记录败退");
                Check(b.GetFieldIds().SequenceEqual(before.Field) && b.GetAllCardIds().SequenceEqual(before.AllCardIds) && card.GetKeywords().SequenceEqual(keywords), "完整战斗不改世界卡与布局");
            }
        }
    }
}
