using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests
{
    /// <summary>K1-K5 / F1-F3：真实场景和 Lua 生命周期、事件身份及冻结回归。</summary>
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
            if (card.Owner.Backpack.HasActivationResources(card)) return;
            foreach (var cost in card.GetManaCost())
            for (int i = 0; i < cost.Value; i++)
            {
                var cell = card.Owner.Backpack.EmptyCells()[0];
                card.Owner.Backpack.PlaceMana(cell, cost.Key);
                Check(card.Owner.Backpack.TryAssign(cell, card), "主动卡可蓄灵");
            }
        }
        private static Card EventCard(Npc npc, int x, int y, bool contest = false)
        {
            var card = Place(npc, contest ? "bp_attack" : "bp_focus", x, y);
            card.BaseData.ScanActivatable = false;
            card.BaseData.Keywords = ["FaBao"];
            card.BaseData.ProducesContest = contest;
            return card;
        }
        private static void Script(CombatCard card, string body)
        {
            card.env = (LuaTable)TestLua.DoString("return {" + body + "}")[0];
            card.env["m_Self"] = card;
            card.env["m_Owner"] = card.Owner;
            card.ScanLuaHooks();
        }
        private static void Keyword(string name, string body) => LuaMgr.Instance.RegisterKeyword(name, (LuaTable)TestLua.DoString("return {" + body + "}")[0]);
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
            Keyword("ActivationProbe", "OnProbe=function(card,ctx) card:TryUseFromEvent(ctx) end");
            RealSwordOpportunities();
            EventEligibility();
            SharedQuotaAndStableOrder();
            IdentityDedupAndLifecycle();
            FrozenScanAndPending();
            EventCyclesAndBudget();
            Console.WriteLine($"ACTIVATION_MIGRATION_TESTS_OK assertions={_assertions}");
        }

        private static void RealSwordOpportunities()
        {
            var a = NewNpc(); var b = NewNpc();
            var source = a.GainEquip("card_fabao_jian", "long_sword");
            a.PlaceBackpackCardInternal(source.Id, 1, 0);
            var scene = Scene(a, b); var sword = Find(scene, a, source);
            Check(!sword.BaseData.HasScanOpportunity && sword.BaseData.HasEventOpportunity && sword.BaseData.HasActiveAbility, "真实长剑纯条件机会仍可蓄灵");
            Check(sword.UseCount == 0 && sword.ApplyCount == 0, "真实法宝没有开场免费使用");
            Light(sword); To(scene, 10);
            Check(sword.UseCount == 0 && sword.Owner.Backpack.LitCount(sword.Id) == 2, "纯法宝扫描不发动");
            scene.GetCombatants()[b.Id].DoManaDraw();
            Check(sword.UseCount == 0, "真实Lua忽略对手抽灵");
            sword.Owner.DoManaDraw();
            Check(sword.UseCount == 1 && sword.Owner.PendingSlot?.SourceCard == sword && sword.Owner.PendingSlot.ContestValue == 4, "主人抽灵使用真实装备Attack=4入槽");
            Check(sword.ApplyCount == 0, "条件入槽不即时Apply");
            To(scene, 50); Check(sword.ApplyCount == 1, "真实长剑本方轮末仅Apply一次"); scene.Cleanup();

            source.BaseData.Keywords.Add("FaShu"); source.BaseData.Keywords.Add("Passive");
            scene = Scene(a, b); sword = Find(scene, a, source);
            Check(sword.UseCount == 0 && sword.ApplyCount == 0 && sword.BaseData.HasScanOpportunity, "真实混合Keyword保留扫描且无开场Apply");
            Light(sword); To(scene, 10);
            Check(sword.UseCount == 1, "真实长剑法术Keyword可扫描发动");
            sword.Owner.DoManaDraw(); Check(sword.UseCount == 1, "待发及已消耗点亮不能由另一机会复用");
            To(scene, 50); Light(sword); sword.Owner.DoManaDraw();
            Check(sword.UseCount == 2 && sword.ApplyCount == 1 && sword.Owner.PendingSlot?.SourceCard == sword, "扫描与事件共用同一实例效果，轮末后事件待发不追补轮末");
            Check(sword.Ticks["CD"] == 0, "混合卡没有旧CD推进");
            scene.Cleanup();
        }

        private static void EventEligibility()
        {
            var a = NewNpc(); var b = NewNpc(); var src = EventCard(a, 1, 0);
            var scene = Scene(a, b); var card = Find(scene, a, src);
            Script(card, "OnProbe=function(self,ctx) self.m_Self:TryUseFromEvent(ctx) end, Contest=function(self,ctx) self.contested=true end, Apply=function(self,ctx) self.applies=(self.applies or 0)+1 end");
            APIContext expired = null;
            WithEvent("OnProbe", ctx => expired = ctx, () => Fire(scene, "OnProbe", card.Owner));
            Check(card.UseCount == 0, "缺灵事件失败");
            Light(card); Check(!card.TryUseFromEvent(expired), "已结束的失败事件不能被保存后补发");
            To(scene, 10); Check(card.UseCount == 0, "迟到满灵不补发旧事件且纯法宝不扫描");
            Fire(scene, "OnProbe", card.Owner);
            Check(card.UseCount == 1 && card.ApplyCount == 1 && card.env["contested"] == null, "无拼点声明跳过空/旧Contest，立即Apply一次");
            card.Apply(); Check(card.ApplyCount == 1, "主动Apply不可重复");
            Light(card); card.Freeze(); Fire(scene, "OnProbe", card.Owner);
            Check(card.UseCount == 1 && card.Owner.Backpack.LitCount(card.Id) == 1, "冻结条件失败不清点亮");
            To(scene, 60); Check(!card.IsFrozen && card.UseCount == 1, "纯法宝冻结也在末格解除且不补发");
            Fire(scene, "OnProbe", card.Owner); Check(card.UseCount == 2, "解冻后的新独立事件可使用");
            scene.Cleanup();

            a = NewNpc(); b = NewNpc(); var attack = Place(a, "bp_attack", 1, 0); src = EventCard(a, 2, 1); var blocked = EventCard(a, 4, 1, true);
            scene = Scene(a, b); card = Find(scene, a, src); var pending = Find(scene, a, attack); var blockedCard = Find(scene, a, blocked);
            Script(card, "OnProbe=function(self,ctx) self.m_Self:TryUseFromEvent(ctx) end, Contest=function(self,ctx) end, Apply=function(self,ctx) end");
            // 保留真正的 bp_attack Contest，只加条件入口。
            blockedCard.BaseData.Keywords.Add("ActivationProbe");
            Light(pending); Light(card); Light(blockedCard); To(scene, 10); Fire(scene, "OnProbe", pending.Owner);
            Check(card.UseCount == 1 && card.ApplyCount == 1 && pending.Owner.PendingSlot?.SourceCard == pending, "空Contest无拼点卡不被其他待发阻挡");
            Check(blockedCard.UseCount == 0 && blockedCard.Owner.Backpack.LitCount(blockedCard.Id) == 1, "有拼点事件槽满失败不消耗");
            scene.Cleanup();
        }

        private static void SharedQuotaAndStableOrder()
        {
            var a = NewNpc(); var b = NewNpc(); var left = Place(a, "bp_focus", 1, 0); var right = EventCard(a, 3, 0);
            var scene = Scene(a, b); var scan = Find(scene, a, left); var conditional = Find(scene, a, right);
            Script(conditional, "OnApply=function(self,ctx) if ctx.SourceCard ~= self.m_Self then self.m_Self:TryUseFromEvent(ctx) end end, Apply=function(self,ctx) end");
            Light(scan); Light(conditional); To(scene, 10);
            Check(scan.UseCount == 1 && conditional.UseCount == 1 && conditional.ApplyCount == 1, "扫描占用行额度后同一行事件仍能发动");
            scene.Cleanup();

            a = NewNpc(); b = NewNpc(); right = EventCard(a, 4, 0, true); left = EventCard(a, 1, 0, true);
            scene = Scene(a, b); var lc = Find(scene, a, left); var rc = Find(scene, a, right);
            foreach (var c in new[] { lc, rc })
            {
                c.BaseData.Keywords.Add("ActivationProbe");
                Light(c);
            }
            lc.Owner.GetField().Reverse(); Fire(scene, "OnProbe", lc.Owner);
            Check(lc.Owner.PendingSlot?.SourceCard == lc && rc.UseCount == 0, "相同事件位置优先于Field顺序和实例创建顺序");
            scene.Cleanup();

            // 右卡先由 Lua/Keyword 请求，左卡后经 EventMgr 请求，统一按位置提交。
            scene = Scene(a, b); lc = Find(scene, a, left); rc = Find(scene, a, right);
            rc.BaseData.Keywords.Add("ActivationProbe"); Light(lc); Light(rc);
            WithEvent("OnProbe", ctx => Check(lc.TryUseFromEvent(ctx), "EventMgr左卡机会已收集"), () => Fire(scene, "OnProbe", lc.Owner));
            Check(lc.Owner.PendingSlot?.SourceCard == lc && rc.UseCount == 0 && rc.Owner.Backpack.LitCount(rc.Id) == 1, "跨通道反序请求仍由左卡入槽，右卡失败保留灵元");
            To(scene, 50);
            Check(rc.UseCount == 0 && rc.Owner.PendingSlot == null && rc.Owner.Backpack.LitCount(rc.Id) == 1, "同事件争槽失败不在槽释放后补发");
            scene.Cleanup();

            // 两个 C# listener 反序注册也不影响争槽。
            scene = Scene(a, b); lc = Find(scene, a, left); rc = Find(scene, a, right); Light(lc); Light(rc);
            WithEvent("OnProbe", ctx => Check(rc.TryUseFromEvent(ctx), "先注册右卡机会已收集"), () =>
                WithEvent("OnProbe", ctx => Check(lc.TryUseFromEvent(ctx), "后注册左卡机会已收集"), () => Fire(scene, "OnProbe", lc.Owner)));
            Check(lc.Owner.PendingSlot?.SourceCard == lc && rc.UseCount == 0, "EventMgr反序注册仍按位置统一提交");
            scene.Cleanup();
        }

        private static void IdentityDedupAndLifecycle()
        {
            Keyword("ActivationFirst", "OnProbe=function(card,ctx) card.env.first=ctx; card:TryUseFromEvent(ctx) end");
            Keyword("ActivationSecond", "OnProbe=function(card,ctx) card.env.second=ctx; card:TryUseFromEvent(ctx) end");
            var a = NewNpc(); var b = NewNpc(); var src = EventCard(a, 1, 0);
            var scene = Scene(a, b); var card = Find(scene, a, src);
            card.BaseData.Keywords.AddRange(["ActivationFirst", "ActivationFirst", "ActivationSecond"]);
            Script(card, "OnProbe=function(self,ctx) self.own=ctx; self.m_Self:TryUseFromEvent(ctx) end, Apply=function(self,ctx) self.effect=ctx; self.refill:Invoke() end");
            card.env["refill"] = (Action)(() => Light(card));
            Light(card);
            APIContext observed = null;
            WithEvent("OnProbe", ctx => { observed = ctx; Check(!card.TryUseFromEvent(ctx), "EventMgr同事件不得二次提交"); }, () => Fire(scene, "OnProbe", scene.GetCombatants()[b.Id]));
            Check(card.UseCount == 1 && card.ApplyCount == 1 && card.Owner.Backpack.LitCount(card.Id) == 1, "同事件多Keyword及双通道仅Use一次，即使Apply重新点满");
            Check(ReferenceEquals(card.env["own"], observed) && ReferenceEquals(card.env["first"], observed) && ReferenceEquals(card.env["second"], observed), "所有通道保留同一实际事件对象");
            var effect = (APIContext)card.env["effect"];
            Check(effect.SourceCard == card && effect.Caster == card.Owner && ReferenceEquals(effect.TriggerEvent, observed) && observed.Caster.Id == b.Id, "效果来源是监听法宝且原始敌方事件另存TriggerEvent");
            var previousId = observed.EventId;
            Fire(scene, "OnProbe", card.Owner);
            Check(card.UseCount == 2 && ((APIContext)card.env["own"]).EventId != previousId, "新独立事件重新点满仍可使用");

            // 受击事件 Caster 是受击者、SourceCard 是攻击卡；Keyword 不得用监听卡改写它们。
            Script(card, "OnDamage=function(self,ctx) if ctx.Caster == self.m_Owner then self.m_Self:TryUseFromEvent(ctx) end end, Apply=function(self,ctx) self.hit=ctx.TriggerEvent end");
            Fire(scene, "OnDamage", scene.GetCombatants()[b.Id]); Check(card.UseCount == 2, "敌方受击不误触发主人受击条件");
            Fire(scene, "OnDamage", card.Owner, scene.GetCombatants()[b.Id].GetField()[0]);
            Check(card.UseCount == 3 && ((APIContext)card.env["hit"]).SourceCard.Owner.Id == b.Id, "主人受击条件保留敌方原攻击来源");
            scene.Cleanup();

            Keyword("ActivationLifecycle", "OnStart=function(card,ctx) card.env.starts=(card.env.starts or 0)+1 end, Start=function(card,ctx) card.env.starts=(card.env.starts or 0)+1 end, OnTick=function(card,ctx) card.env.kt=(card.env.kt or 0)+1 end, Tick=function(card,ctx) card.env.kt=(card.env.kt or 0)+1 end");
            a = NewNpc(); b = NewNpc(); src = EventCard(a, 1, 0); src.BaseData.Keywords.AddRange(["FaShu", "Passive", "ActivationLifecycle"]);
            scene = new CombatScene(); scene.Init(7119, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 2000); scene.PreStart();
            card = Find(scene, a, src);
            Script(card, "Tick=function(self,ctx) self.t=(self.t or 0)+1 end, OnPassiveTick=function(self,ctx) self.pt=(self.pt or 0)+1 end");
            scene.Start();
            Check(Convert.ToInt32(card.env["starts"]) == 1 && card.UseCount == 0 && card.ApplyCount == 0, "Start/OnStart二选一且混合Passive无免费Apply");
            card.Freeze(); Light(card); To(scene, 3);
            Check(Convert.ToInt32(card.env["kt"]) == 3 && Convert.ToInt32(card.env["t"]) == 3 && Convert.ToInt32(card.env["pt"]) == 3, "混合卡冻结时主动Tick和被动回调各一次");
            Check(card.IsFrozen && card.UseCount == 0 && card.Ticks["CD"] == 0, "冻结不阻蓄灵/被动，旧CD仍禁用");
            scene.Cleanup();

            a = NewNpc(); b = NewNpc();
            // 纯被动明确声明 Passive，不由 Cooldown 推断。
            var mainId = a.GetCardLoadoutSnapshot().MainGongFaCardId.Value;
            a.GetAllCards().Single(c => c.Id == mainId).BaseData.Keywords.Add("Passive");
            scene = Scene(a, b); card = scene.GetCombatants()[a.Id].GetField().Single(c => c.Id == mainId);
            Check(card.CheckPhase(CombatCardPhase.Passive) && card.UseCount == 0 && card.ApplyCount == 1, "显式纯Passive保留开场一次Apply");
            scene.Cleanup();
        }

        private static void FrozenScanAndPending()
        {
            var a = NewNpc(); var b = NewNpc(); var left = Place(a, "bp_focus", 1, 0); var right = Place(a, "bp_focus", 3, 0);
            var scene = Scene(a, b); var lc = Find(scene, a, left); var rc = Find(scene, a, right);
            Light(lc); Light(rc); Check(lc.Freeze() && !lc.Freeze(), "冻结不叠加"); To(scene, 10);
            Check(!lc.IsFrozen && lc.UseCount == 0 && lc.Owner.Backpack.LitCount(lc.Id) == 1 && rc.UseCount == 1, "满灵左卡冻结跳过解冻，右卡使用"); scene.Cleanup();

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
            Light(card); To(scene, 60); Check(!card.IsFrozen && card.UseCount == 1 && card.Owner.Backpack.LitCount(card.Id) == 1, "已待发冻结在下次完整扫描跳过解冻");
            scene.Cleanup();
        }

        private static void EventCyclesAndBudget()
        {
            var a = NewNpc(); var b = NewNpc(); var src = EventCard(a, 1, 0);
            var scene = Scene(a, b); var card = Find(scene, a, src);
            Script(card, "OnProbe=function(self,ctx) self.m_Self:TryUseFromEvent(ctx) end, OnUse=function(self,ctx) self.m_Self:TryUseFromEvent(ctx) end, Apply=function(self,ctx) self.refill:Invoke() end");
            card.env["refill"] = (Action)(() => Light(card)); Light(card); Fire(scene, "OnProbe", card.Owner);
            Check(card.UseCount == 1 && !scene.IsFinished && card.Owner.Backpack.LitCount(card.Id) == 1, "自身Use派生事件拒绝重入且不消耗重新点亮");
            Fire(scene, "OnProbe", card.Owner); Check(card.UseCount == 2, "拒绝自身派生不吞独立事件"); scene.Cleanup();

            // A→B→缺灵A 是自然终止链；补满A后相同路径才是真正循环。
            foreach (var refill in new[] { false, true })
            {
                a = NewNpc(); b = NewNpc(); var one = EventCard(a, 1, 0); var two = EventCard(a, 3, 0);
                scene = Scene(a, b); var ca = Find(scene, a, one); var cb = Find(scene, a, two);
                Script(ca, "OnProbe=function(self,ctx) self.m_Self:TryUseFromEvent(ctx) end, OnUse=function(self,ctx) if ctx.SourceCard ~= self.m_Self then self.m_Self:TryUseFromEvent(ctx) end end, Apply=function(self,ctx) if self.refill then self.refill:Invoke() end end");
                Script(cb, "OnUse=function(self,ctx) if ctx.SourceCard ~= self.m_Self then self.m_Self:TryUseFromEvent(ctx) end end, Apply=function(self,ctx) end");
                if (refill) ca.env["refill"] = (Action)(() => Light(ca));
                Light(ca); Light(cb); bool rejected = false;
                try { Fire(scene, "OnProbe", ca.Owner); }
                catch (InvalidOperationException ex) { rejected = ex.Message.Contains("主动事件循环") && ex.Message.Contains("OnUse"); }
                Check(refill ? rejected && scene.IsFinished : !rejected && !scene.IsFinished && ca.UseCount == 1 && cb.UseCount == 1, refill ? "有资源的A-B-A异常链明确终止并输出来源" : "缺灵A-B-A链不被误判异常");
                scene.Cleanup();
            }

            a = NewNpc(); b = NewNpc(); scene = Scene(a, b); var owner = scene.GetCombatants()[a.Id];
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
