using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests
{
    /// <summary>Main Keyword 的末格发动、法宝生命周期与真实对拼回归。</summary>
    public static class ActivationMigrationTests
    {
        private static int _assertions;
        private static readonly Lua TestLua = new();
        private static void Check(bool condition, string message)
        {
            _assertions++;
            if (!condition) throw new InvalidOperationException("主动机会断言失败：" + message);
        }
        private static Npc NewNpc()
        {
            var npc = BackpackModelTests.CreateNpc();
            npc.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "activation-test"));
            return npc;
        }
        private static Card Place(Npc npc, string id, int x, int y) => BackpackModelTests.Place(npc, id, x, y);
        private static CombatScene Scene(Npc a, Npc b)
        {
            var scene = new CombatScene();
            scene.Init(7119, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 2000);
            scene.EnablePresentationRecording(true);
            scene.PreStart(); scene.Start();
            return scene;
        }
        private static CombatCard Find(CombatScene scene, Npc npc, Card card) => scene.GetCombatants()[npc.Id].GetField().Single(c => c.Id == card.Id);
        private static void To(CombatScene scene, int tick)
        {
            while (!scene.IsFinished && scene.CurrentTick < tick) scene.Tick();
        }
        private static void Light(CombatCard card)
        {
            if (card.HasActivationResources()) return;
            foreach (var cost in card.GetManaCost())
            for (int i = 0; i < cost.Value; i++)
            {
                var cell = card.Owner.Backpack.EmptyCells()[0];
                card.Owner.Backpack.PlaceMana(cell, cost.Key);
                Check(card.Owner.Backpack.TryAssign(cell, card), "主动卡可蓄灵");
            }
        }
        private static Card PassiveArtifact(Npc npc, int x, int y)
        {
            var card = Place(npc, "bp_focus", x, y);
            card.BaseData.Keywords = ["FaBao"];
            card.BaseData.ProducesContest = false;
            return card;
        }
        private static void Script(CombatCard card, string body)
        {
            card.env = (LuaTable)TestLua.DoString("return {" + body + "}")[0];
            card.env["m_Self"] = card;
            card.env["m_Owner"] = card.Owner;
            card.ScanLuaHooks();
        }
        private static void WithEvent(string name, Action<APIContext> callback, Action action)
        {
            var listener = new DelegateEventListener((_, _, args) => callback((APIContext)args));
            EventMgr.Instance.RegisterEvent("activation-test:" + name, name, ScopeKey.Global, listener);
            try { action(); }
            finally { EventMgr.Instance.RemoveEvent(name, ScopeKey.Global, listener); }
        }
        private static void Fire(CombatScene scene, string name, CombatNpc source, CombatCard card = null) => scene.TriggerCombatEvent(name, new APIContext { Scene = scene, Caster = source, SourceCard = card });

        public static void Run()
        {
            _assertions = 0;
            LongSwordBattle();
            PassiveWindowAndEventIdentity();
            SharedRowQuota();
            FrozenScanAndPending();
            EventCyclesAndBudget();
            Console.WriteLine($"ACTIVATION_MIGRATION_TESTS_OK assertions={_assertions}");
        }

        private static void LongSwordBattle()
        {
            var a = NewNpc();
            var b = NewNpc();
            var equipment = a.GainEquip("card_fabao_jian", "long_sword");
            a.EquipFaBao(equipment.Id);
            a.PlaceBackpackCardInternal(equipment.Id, 1, 0);
            var thrust = a.GetAllCards().Single(c => c.ParentCardId == equipment.Id && c.DefineId == "card_form_jian_ci");
            var slash = a.GetAllCards().Single(c => c.ParentCardId == equipment.Id && c.DefineId == "card_form_jian_zhan");
            a.PlaceBackpackCardInternal(thrust.Id, 1, 3);
            a.PlaceBackpackCardInternal(slash.Id, 1, 4);
            var slashes = new[] { Place(a, "bp_attack", 1, 1), Place(a, "bp_attack", 1, 2) };
            var guards = Enumerable.Range(1, 4).Select(row => Place(b, "bp_guard", 1, row)).ToArray();
            var scene = Scene(a, b);
            var sword = Find(scene, a, equipment);
            var thrustCard = Find(scene, a, thrust);
            var slashCard = Find(scene, a, slash);
            Check(thrustCard.GetEquipData().GetValue("Attack", 0) == 4 && thrustCard.GetEquipData().GetValue("Defend", 0) == 3,
                "父法宝未激活时已绑定的招式可读真实攻防");
            foreach (var card in slashes.Select(c => Find(scene, a, c)).Concat(guards.Select(c => Find(scene, b, c))).Append(sword).Append(thrustCard).Append(slashCard))
                Light(card);
            sword.Owner.DoManaDraw();
            Check(!sword.IsPassiveActive && sword.UseCount == 0 && sword.LitCount == 2 && sword.Owner.PendingSlot == null,
                "主人抽灵不能直接发动长剑");
            To(scene, 10);
            Check(sword.IsPassiveActive && sword.UseCount == 1 && sword.LitCount == 0 && sword.Owner.PendingSlot == null,
                "长剑满灵扫到末格开启被动，不产生拼点");
            Check(thrustCard.GetEquipData().GetValue("Attack", 0) == 4 && thrustCard.GetEquipData().GetValue("Defend", 0) == 3,
                "长剑激活后绑定招式仍读取同一份装备攻防");
            To(scene, 30);
            var hit = new ContestData { SourceCard = thrustCard, ContestType = ContestType.Ci, ContestValue = 4, Element = ElementType.Jin };
            sword.Owner.Straight(hit);
            Check(hit.ContestValue == 4, "直击保留装备原点数");
            To(scene, 50);
            var values = scene.ReadPresentationEvents().Where(e => e.Contest != null)
                .Select(e => e.Contest.Left.NpcId == a.Id ? e.Contest.Left.Value : e.Contest.Right.Value).ToArray();
            Check(values.SequenceEqual(new[] { 6, 6, 5, 4 }), "两次斩击获胜只存一份加值，下一次实际招式对拼使用一次");
            Check(thrustCard.UseCount == 1 && slashCard.UseCount == 1, "剑刺和剑斩分别发动");
            var victory = new APIContext { Caster = sword.Owner, SourceCard = slashCard };
            victory.Set("WinnerTypeName", "Zhan");
            scene.TriggerCombatEvent("OnContestOverflow", victory);
            Check(sword.env["nextFormBonus"] is true, "激活期记录新胜利");
            To(scene, 60);
            Check(!sword.IsPassiveActive && sword.UseCount == 1 && sword.env["nextFormBonus"] is false, "没能续期时清理加值");
            scene.TriggerCombatEvent("OnContestOverflow", victory);
            Check(sword.env["nextFormBonus"] is false, "失活后不领取加值");
            scene.Cleanup();
        }

        private static void PassiveWindowAndEventIdentity()
        {
            var a = NewNpc();
            var b = NewNpc();
            var attack = Place(a, "bp_attack", 1, 0);
            var artifact = PassiveArtifact(a, 2, 1);
            var scene = Scene(a, b);
            var card = Find(scene, a, artifact);
            var enemy = scene.GetCombatants()[b.Id];
            Script(card, "OnPassiveActivated=function(self,ctx) self.starts=(self.starts or 0)+1 end," +
                "OnPassiveDeactivated=function(self,ctx) self.stops=(self.stops or 0)+1 end," +
                "OnProbe=function(self,ctx) self.seen=ctx end");
            var enemyCard = enemy.GetField()[0];
            Fire(scene, "OnProbe", enemy, enemyCard);
            Check(card.env["seen"] == null, "未激活不监听事件");
            Light(Find(scene, a, attack));
            Light(card);
            To(scene, 20);
            Check(card.IsPassiveActive && card.Owner.PendingSlot?.SourceCard.Id == attack.Id, "满待发槽仍可激活法宝");
            APIContext observed = null;
            WithEvent("OnProbe", ctx => observed = ctx, () => Fire(scene, "OnProbe", enemy, enemyCard));
            Check(observed != null && ReferenceEquals(card.env["seen"], observed) && observed.Caster == enemy && observed.SourceCard == enemyCard,
                "激活期收到实际事件的施放者和来源");
            Light(card);
            To(scene, 70);
            Check(card.IsPassiveActive && card.UseCount == 2 && Convert.ToInt32(card.env["starts"]) == 1 && card.env["stops"] == null,
                "激活期可蓄灵并连续续期，激活回调只执行一次");
            Light(card);
            card.Freeze();
            card.Owner.Backpack.AddHaste(100);
            To(scene, 95);
            Check(!card.IsPassiveActive && !card.IsFrozen && card.UseCount == 2 && card.LitCount == 1 &&
                Convert.ToInt32(card.env["stops"]) == 1, "加速后的实际末格仍会处理冻结解冻和失败到期");
            To(scene, 120);
            Check(card.IsPassiveActive && card.UseCount == 3, "下次完整扫描可重新激活");
            card.Owner.RemoveCombatCard(card);
            card.Owner.DealFieldChange();
            Check(!card.IsPassiveActive && Convert.ToInt32(card.env["stops"]) == 2, "离场结束激活并执行清理");
            scene.Cleanup();
        }

        private static void SharedRowQuota()
        {
            var a = NewNpc();
            var b = NewNpc();
            var spell = Place(a, "bp_focus", 1, 0);
            var artifact = PassiveArtifact(a, 2, 0);
            var weapon = a.GainEquip("card_fabao_jian", "short_sword");
            a.EquipFaBao(weapon.Id);
            var thrust = a.GetAllCards().Single(c => c.ParentCardId == weapon.Id && c.DefineId == "card_form_jian_ci");
            a.PlaceBackpackCardInternal(thrust.Id, 3, 0);
            var scene = Scene(a, b);
            var fabao = Find(scene, a, artifact);
            var form = Find(scene, a, thrust);
            var left = Find(scene, a, spell);
            Light(fabao);
            Light(form);
            To(scene, 10);
            Check(fabao.UseCount == 1 && form.UseCount == 0 && form.LitCount == 1, "法宝先发动会占用招式的同行额度");
            Light(fabao);
            Light(left);
            To(scene, 60);
            Check(left.UseCount == 1 && fabao.UseCount == 1 && !fabao.IsPassiveActive && fabao.LitCount == 1 && form.UseCount == 0,
                "法术先发动时右侧法宝到期但保留灵元，招式也等待");
            scene.Cleanup();
        }

        private static void FrozenScanAndPending()
        {
            var a = NewNpc(); var b = NewNpc(); var left = Place(a, "bp_focus", 1, 0); var right = Place(a, "bp_focus", 3, 0);
            var scene = Scene(a, b); var lc = Find(scene, a, left); var rc = Find(scene, a, right);
            Light(lc); Light(rc); Check(lc.Freeze() && !lc.Freeze(), "冻结不叠加"); To(scene, 10);
            Check(!lc.IsFrozen && lc.UseCount == 0 && lc.LitCount == 1 && rc.UseCount == 1, "满灵左卡冻结跳过解冻，右卡使用"); scene.Cleanup();

            scene = Scene(a, b); lc = Find(scene, a, left); rc = Find(scene, a, right);
            Light(lc); rc.Freeze(); To(scene, 10);
            Check(lc.UseCount == 1 && !rc.IsFrozen && rc.UseCount == 0, "缺灵且行额度已用的右卡仍在末格解冻"); scene.Cleanup();

            a = NewNpc(); b = NewNpc(); var multi = Place(a, "bp_charge", 2, 0);
            scene = Scene(a, b); var card = Find(scene, a, multi); card.Freeze();
            To(scene, 20); Check(card.IsFrozen, "多格冻结非末格不解冻"); To(scene, 30);
            Check(!card.IsFrozen && card.UseCount == 0, "跨行末格缺灵仍解冻"); scene.Cleanup();

            a = NewNpc(); b = NewNpc(); left = Place(a, "bp_attack", 1, 0);
            scene = Scene(a, b); card = Find(scene, a, left); Light(card); To(scene, 10); card.Freeze(); To(scene, 50);
            Check(card.ApplyCount == 1 && card.IsFrozen && card.Owner.PendingSlot == null, "待发被冻结仍正常结算且保留冻结");
            Light(card); To(scene, 60); Check(!card.IsFrozen && card.UseCount == 1 && card.LitCount == 1, "已待发冻结在下次完整扫描跳过解冻");
            scene.Cleanup();
        }

        private static void EventCyclesAndBudget()
        {
            var a = NewNpc(); var b = NewNpc(); var scene = Scene(a, b); var owner = scene.GetCombatants()[a.Id];
            var seen = 0;
            WithEvent("OnStress", ctx => { seen++; if (seen < 5000) Fire(scene, "OnStress", owner); }, () => Fire(scene, "OnStress", owner));
            Check(seen == 5000 && scene.LargestEventChain == 5000 && !scene.IsFinished, "5000事件链无递归栈增长或静默丢失");
            for (int i = 0; i < 100; i++) Fire(scene, "OnStress", owner);
            Check(!scene.IsFinished, "独立事件不累计为旧链预算");
            bool budgetStopped = false;
            WithEvent("OnLoop", ctx => Fire(scene, "OnLoop", owner), () =>
            {
                try { Fire(scene, "OnLoop", owner); }
                catch (InvalidOperationException ex) { budgetStopped = ex.Message.Contains("10000") && ex.Message.Contains("OnLoop"); }
            });
            Check(budgetStopped && scene.IsFinished && scene.EventFailure != null, "无主动Use的无限事件链超过预算也明确停止");
            scene.Cleanup();
            Console.WriteLine("ACTIVATION_EVENT_PRESSURE finite=5000 independent=100 budget=10000 diagnostics=64");
        }
    }
}
