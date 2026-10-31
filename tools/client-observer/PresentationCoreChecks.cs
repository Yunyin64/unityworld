using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;
using UnityWorld.Game.World;

namespace Xianxia.Client.Battle;

/// <summary>真实内存战斗与 Lua 边界测试；不修改正式模板、卡定义或 Lua 文件。</summary>
internal static class PresentationCoreChecks
{
    private static int _assertions;
    private static Lua _lua = null!;
    private static void Check(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException("表现核心断言失败：" + message);
    }
    public static void Run(string root)
    {
        _assertions = 0;
        using var lua = new Lua(); _lua = lua;
        WorldMgr.Initialize(48031, Path.Combine(root, "UnityWorld"));
        try
        {
            WorldMgr.Start();
            ManaUseAndPending();
            ContestsAndStraight();
            DamageAndPlacement();
            PassiveCardEntry();
            PassiveOwnership();
            BufferLifecycle();
            Console.WriteLine($"PRESENTATION_CORE_OK assertions={_assertions}");
        }
        finally { WorldMgr.End(); }
    }
    private static Npc NewNpc()
    {
        var npc = NpcMgr.Instance.AssembleGenerated(new NpcDefine { ID = "presentation-test", InitStat = new()
        { ["QiXue"] = 10, ["TiPo"] = 10, ["QiGan"] = 10, ["LingJi"] = 10, ["ShenShi"] = 10, ["WuXing"] = 10, ["JiYuan"] = 10, ["MeiLi"] = 10 } });
        var main = npc.GainCard("bp_main_6x5");
        GongFaMgr.Instance.InstantiateFromDefine(main.Id, CultivationDefineMgr.Instance.Get("bp_main_6x5"));
        npc.SelectMainGongFa(main.Id);
        npc.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "presentation-test"));
        return npc;
    }
    private static Card Place(Npc npc, string id, int x, int y)
    { var card = npc.GainCard(id); npc.PlaceBackpackCardInternal(card.Id, x, y); return card; }
    private static CombatScene Scene(Npc a, Npc b, bool observe = true)
    {
        var scene = new CombatScene();
        scene.Init(48031, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 2000);
        scene.EnablePresentationRecording(observe);
        scene.PreStart(); scene.Start(); scene.DrainPresentationEvents();
        return scene;
    }
    private static CombatCard Card(CombatScene scene, Npc owner, Card source) => scene.GetCombatants()[owner.Id].GetField().Single(c => c.Id == source.Id);
    private static void To(CombatScene scene, int tick) { while (scene.CurrentTick < tick && !scene.IsFinished) scene.Tick(); }
    private static void Light(CombatCard card)
    {
        foreach (var cost in card.GetManaCost())
            while (card.Owner.Backpack.AssignedTo(card.Id).Count(e => e.Equals(cost.Key)) < cost.Value)
            {
                var cell = card.Owner.Backpack.EmptyCells()[0];
                card.Owner.Backpack.PlaceMana(cell, cost.Key);
                Check(card.Owner.Backpack.TryAssign(cell, card), "测试补灵成功");
            }
    }
    private static void Script(CombatCard card, string body)
    {
        card.env = (LuaTable)_lua.DoString("return {" + body + "}")[0];
        card.env["m_Self"] = card; card.env["m_Owner"] = card.Owner;
        card.ScanLuaHooks();
    }
    private static void ManaUseAndPending()
    {
        var a = NewNpc(); var b = NewNpc();
        var source1 = Place(a, "bp_attack", 2, 0); var source2 = Place(a, "bp_attack", 4, 0);
        var scene = Scene(a, b); var one = Card(scene, a, source1); var two = Card(scene, a, source2);
        // 两个真实源格在各自卡前，同一行依次分配；本行成功额度阻止第二张使用。
        one.Owner.Backpack.PlaceMana(new(1, 0), ElementType.Jin);
        one.Owner.Backpack.PlaceMana(new(3, 0), ElementType.Jin);
        To(scene, 10);
        var events = scene.DrainPresentationEvents();
        var assigned = events.Where(e => e.Kind == CombatPresentationKind.ManaAssigned).ToArray();
        Check(assigned.Length == 2 && assigned[0].Card!.Id == one.Id && assigned[1].Card!.Id == two.Id, "同名实例按实际分配目标区分");
        Check(assigned[0].ManaAssigned!.SourceCell == new PresentationCell(1, 0) && assigned[1].ManaAssigned!.SourceCell == new PresentationCell(3, 0), "两个灵元真实零基源格");
        var used = events.Single(e => e.Kind == CombatPresentationKind.CardUsed);
        Check(used.Tick == 10 && assigned[0].Sequence < used.Sequence && used.CardUsed!.Before.Count == 1 && used.CardUsed.After.Count == 0, "同 Tick 分配后使用清空保留顺序");
        Check(one.Owner.Backpack.LitCount(one.Id) == 0 && one.Owner.Backpack.LitCount(two.Id) == 1, "最终点亮与中间记录独立");
        Check(events.Single(e => e.Kind == CombatPresentationKind.PendingChanged).ActionId == used.ActionId, "使用与真实入槽关联");
        Check(!events.Any(e => e.Kind == CombatPresentationKind.CardApplied), "只待发不提前生效");
        one.Owner.Backpack.PlaceMana(new(5, 0), ElementType.Huo);
        Check(one.Owner.ManaTargetSelector.AssignAt(one.Owner.Backpack, new(5, 0), one.Owner.GetField()) == null, "不匹配灵元留格");
        Check(scene.ReadPresentationEvents().Count == 0, "未分配不伪造事件");
        // 通过正式事件机会验证槽满时不产生 Use；不直接调用内部提交入口。
        two.BaseData.Keywords.Add("FaBao");
        two.BaseData.ProducesContest = true;
        Script(two, "OnProbe=function(self,ctx) self.m_Self:TryUseFromEvent(ctx) end");
        scene.TriggerCombatEvent("OnProbe", new APIContext { Scene = scene, Caster = one.Owner });
        Check(!scene.ReadPresentationEvents().Any(e => e.Kind == CombatPresentationKind.CardUsed) && one.Owner.Backpack.LitCount(two.Id) == 1, "槽满拒绝且保留点亮");
        Check(assigned[0].Card!.AssignedElements.Count == 1 && assigned[0].ManaAssigned!.After.Count == 1, "历史值不会被使用清空");
        scene.Cleanup();
    }
    private static void ContestsAndStraight()
    {
        foreach (var equal in new[] { true, false })
        {
            var a = NewNpc(); var b = NewNpc(); var ca = Place(a, "bp_attack", 1, 0); var cb = Place(b, "bp_attack", 1, 0);
            var scene = Scene(a, b); var one = Card(scene, a, ca); var two = Card(scene, b, cb);
            // 使用真实 Lua Contest；非平局在入槽后改拼点测试结算，不改生产数据。
            Light(one); Light(two);
            if (!equal)
            {
                Script(two, "Contest=function(self,ctx) self.m_Self:TryPushToPendingSlot(self.kind, self.element, 3) end");
                two.env["kind"] = ContestType.Zhan; two.env["element"] = ElementType.Jin;
            }
            To(scene, 10);
            var events = scene.DrainPresentationEvents();
            var contest = events.Single(e => e.Kind == CombatPresentationKind.ContestResolved);
            Check(new[] { contest.Contest!.Left.Card.Id, contest.Contest.Right.Card.Id }.ToHashSet().SetEquals(new[] { one.Id, two.Id }), "真实双待发卡身份");
            Check(contest.Contest.Left.ActionId != null && contest.Contest.Right.ActionId != null, "双方独立使用关联");
            Check((contest.Contest.WinnerNpcId == null) == equal, "平局早返回与胜负均有记录");
            var applied = events.Where(e => e.Kind == CombatPresentationKind.CardApplied && (e.Card!.Id == one.Id || e.Card.Id == two.Id)).ToArray();
            Check(applied.Length == 2 && applied.All(e => e.ContestId == contest.ContestId && e.Sequence > contest.Sequence), "赢家输家和平局双方 Apply 归属同一对拼");
            Check(one.Owner.PendingSlot == null && two.Owner.PendingSlot == null && events.Count(e => e.Kind == CombatPresentationKind.PendingChanged) == 4, "同 Tick 双入槽双出槽，终态空");
            var damage = events.Where(e => e.Kind == CombatPresentationKind.DamageResolved).ToArray();
            var winnerAction = contest.Contest.Left.NpcId == contest.Contest.WinnerNpcId ? contest.Contest.Left.ActionId : contest.Contest.Right.ActionId;
            Check(equal ? damage.Length == 0 : damage.Length == 1 && damage[0].ContestId == contest.ContestId && damage[0].ActionId == winnerAction, "延迟伤害保留对拼使用来源");
            scene.Cleanup();
        }
        {
            var a = NewNpc(); var b = NewNpc(); var ca = Place(a, "bp_attack", 1, 0);
            var scene = Scene(a, b); var one = Card(scene, a, ca); Light(one); To(scene, 50);
            var events = scene.DrainPresentationEvents();
            Check(!events.Any(e => e.Kind == CombatPresentationKind.ContestResolved), "单方直击不伪造对手");
            Check(events.Any(e => e.Kind == CombatPresentationKind.DamageResolved && e.ContestId == null), "直击伤害保留无对拼关联");
            scene.Cleanup();
        }
        {
            var a = NewNpc(); var b = NewNpc(); var ca = Place(a, "bp_attack", 1, 0); var cb = Place(b, "bp_attack", 1, 0);
            var cr = Place(a, "bp_attack", 1, 1); cr.BaseData.ScanActivatable = false; cr.BaseData.Keywords = ["FaBao"];
            var scene = Scene(a, b); var one = Card(scene, a, ca); var two = Card(scene, b, cb); var refill = Card(scene, a, cr);
            Script(refill, "OnApply=function(self,ctx) if ctx.Caster==self.m_Owner then self.m_Self:TryUseFromEvent(ctx) end end, Contest=function(self,ctx) self.m_Self:TryPushToPendingSlot(self.kind,self.element,4) end");
            refill.env["kind"] = ContestType.Zhan; refill.env["element"] = ElementType.Jin;
            Light(one); Light(two); Light(refill); To(scene, 10);
            var events = scene.DrainPresentationEvents(); var contest = events.Single(e => e.Contest != null);
            var newEntry = events.Single(e => e.Pending?.Entered == true && e.Card!.Id == refill.Id);
            Check(one.Owner.PendingSlot?.SourceCard == refill && newEntry.Sequence > contest.Sequence, "同 Tick 对拼后新卡补槽完整保留");
            Check(!events.Any(e => e.Applied != null && e.Card!.Id == refill.Id), "补槽卡尚未生效不提前弹名");
            scene.Cleanup();
        }
    }
    private static void DamageAndPlacement()
    {
        var a = NewNpc(); var b = NewNpc(); var scene = Scene(a, b); var target = scene.GetCombatants()[b.Id];
        target.ChangeShield(7);
        target.ApplyDamage(new DamageInfo { Damage = 4 });
        target.ApplyDamage(new DamageInfo { Damage = 5 });
        var initial = scene.DrainPresentationEvents().Where(e => e.Damage != null).ToArray();
        Check(initial.Length == 2 && initial[0].Damage!.HpDamage == 0 && initial[0].Damage!.ShieldAbsorbed == 4, "全盾吸收独立事实");
        Check(initial[1].Damage!.HpDamage == 2 && initial[1].Damage!.ShieldAbsorbed == 3, "半盾吸收与实际扣血");
        Check(initial.All(e => e.Damage!.SourceNpcId == null && e.Damage.SourceCard == null), "无来源伤害合法");
        target.AddModifier("Zhan_Resist", 2);
        target.ApplyDamage(new DamageInfo { Damage = 5, damageType = DamageType.Zhan });
        var reduced = scene.DrainPresentationEvents().Single(e => e.Damage != null);
        Check(reduced.Damage!.HpDamage == 3, "DamageIn 减伤后的金额");
        target.RemoveModifier("Zhan_Resist");
        var oldHp = target.GetHp(); target.ApplyDamage(new DamageInfo { Damage = oldHp + 5 });
        var lethal = scene.ReadPresentationEvents().Single(e => e.Damage != null);
        Check(lethal.Damage!.HpBefore == oldHp && lethal.Damage.HpAfter == -5 && lethal.Damage.HpDamage == oldHp + 5, "超额扣血事实在恢复前保存");
        Check(target.GetHp() > 0 && !scene.ReadPresentationEvents().Any(e => e.CardAdded != null), "伤口请求时不提前发布入包");
        target.DealFieldChange();
        var wound = scene.DrainPresentationEvents().Single(e => e.CardAdded != null);
        Check(wound.Card!.Keywords.Contains("Wound") && wound.Card.Cells.Count == wound.Card.Size && wound.Card.Shape.Count == wound.Card.Size, "伤口成功实际占格完整");
        var regular = CombatCard.CreateFromData(CardMgr.Instance.InstantiateFromDefine("bp_charge"));
        target.AddCombatCard(regular); Check(scene.ReadPresentationEvents().Count == 0, "普通卡仅排队不报成功");
        target.DealFieldChange(); var added = scene.DrainPresentationEvents().Single(e => e.CardAdded != null);
        Check(added.Card!.Id == regular.Id && added.Card.Cells.Count == 3 && added.Card.ManaCost.Sum(c => c.Count) == 3, "通用多格卡完整值");
        target.RemoveCombatCard(regular); target.DealFieldChange();
        Check(!target.Backpack.Placements.ContainsKey(regular.Id) && added.Card.Cells.Count == 3, "后续移除不污染历史值");
        foreach (var cell in target.Backpack.EmptyCells()) target.Backpack.PlaceMana(cell, ElementType.Shui);
        var blocked = CombatCard.CreateFromData(CardMgr.Instance.InstantiateFromDefine("bp_focus"));
        target.AddCombatCard(blocked);
        var rejected = false; try { target.DealFieldChange(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && !scene.ReadPresentationEvents().Any(e => e.CardAdded != null), "普通卡失败不发布成功落位");
        var replacement = target.AddWound("bp_wound_small"); target.DealFieldChange();
        var replace = scene.DrainPresentationEvents().Single(e => e.CardAdded != null);
        Check(replace.Card!.Id == replacement.Id && replace.CardAdded!.ReplacedMana.Count == 1 && replace.CardAdded.ReplacedMana[0].Element == ElementType.Shui.ToString(), "伤口替代真实灵元，不伪造吸收");
        scene.Cleanup();
    }
    private static void PassiveCardEntry()
    {
        var a = NewNpc(); var b = NewNpc(); var scene = Scene(a, b);
        To(scene, 1); scene.DrainPresentationEvents();
        var owner = scene.GetCombatants()[a.Id];
        var card = CombatCard.CreateFromData(CardMgr.Instance.InstantiateFromDefine("card_monster_langya"));
        owner.AddCombatCard(card);
        Check(scene.ReadPresentationEvents().Count == 0, "新被动卡请求时既不提前生效也不提前入包");
        owner.DealFieldChange();
        var events = scene.DrainPresentationEvents();
        var applied = events.Single(e => e.Applied != null && e.Card!.Id == card.Id);
        var added = events.Single(e => e.CardAdded != null && e.Card!.Id == card.Id);
        Check(applied.Sequence < added.Sequence && applied.Tick == 1 && added.Tick == 1 && !added.IsInitialization,
            "战中新被动卡 Start 的真实 Apply 事实先于入场流程完成后的 Added");
        Check(applied.NpcId == owner.Id && added.NpcId == owner.Id && applied.Card!.DefineId == "card_monster_langya" && applied.Applied!.IsPassive,
            "真实新增被动卡的执行身份与入包所属者一致");
        Check(owner.GetAllModifiers().Any(m => m.DefineId == "Ci_Improved") && owner.Backpack.Placements[card.Id].Count == added.Card!.Cells.Count,
            "真实 Passive.Start 已提交强化，入包记录包含实际成功占格");
        Check(events.Count(e => e.CardAdded != null) == 1 && events.Count(e => e.Applied != null && e.Card!.Id == card.Id) == 1,
            "新被动卡一次生效与一次入包，关键词外层不重复提示");
        scene.Cleanup();
    }
    private static void PassiveOwnership()
    {
        var a = NewNpc(); var b = NewNpc(); var ca = Place(a, "bp_focus", 1, 0); var cb = Place(b, "bp_focus", 1, 0);
        var scene = Scene(a, b); var one = Card(scene, a, ca); var two = Card(scene, b, cb);
        Script(one, "Probe=function(self,ctx) local x=self.m_Owner:GetHp() end");
        one.CallLua("Probe"); Check(scene.ReadPresentationEvents().Count == 0, "空条件检查不生效");
        Script(one, "Probe=function(self,ctx) self.m_Owner:ChangeShield(0); self.m_Owner:RecoverHP(0); self.m_Owner.Backpack:Charge(0) end");
        one.CallLua("Probe"); Check(scene.ReadPresentationEvents().Count == 0, "零动作不弹名");
        Script(one, "Probe=function(self,ctx) self.m_Owner:ChangeShield(2); self.m_Owner:RecoverHP(1) end");
        one.CallLua("Probe", new APIContext { Caster = two.Owner, SourceCard = two, Scene = scene });
        var passive = scene.DrainPresentationEvents().Single();
        Check(passive.Kind == CombatPresentationKind.CardApplied && passive.NpcId == one.Owner.Id && passive.Card!.Id == one.Id && passive.Applied!.IsPassive, "多动作一次生效，归属执行卡而非 ctx.SourceCard");
        Script(two, "Probe=function(self,ctx) self.m_Owner:ChangeShield(3) end");
        Script(one, "Probe=function(self,ctx) self.m_Owner:ChangeShield(1); self.other:CallLua('Probe',ctx); self.m_Owner:RecoverHP(1) end");
        one.env["other"] = two; one.CallLua("Probe");
        var nested = scene.DrainPresentationEvents();
        Check(nested.Count == 2 && nested.Select(e => e.Card!.Id).Distinct().Count() == 2 && nested.Select(e => e.EffectId).Distinct().Count() == 2, "嵌套两张同名卡各自一次独立生效");
        // 关键词路径独立于卡自身 Hook；直接实体动作也被标记。
        LuaMgr.Instance.RegisterKeyword("PresentationProbe", (LuaTable)_lua.DoString("return {Probe=function(card,ctx) card.Owner.Backpack:AddHaste(2) end}")[0]);
        one.BaseData.Keywords.Add("PresentationProbe"); Script(one, "Probe=function(self,ctx) end"); one.CallLua("Probe");
        var keyword = scene.DrainPresentationEvents().Single();
        Check(keyword.Applied!.Origin == "Keyword:PresentationProbe:Probe", "关键词实际提交被观察");
        Script(one, "Probe=function(self,ctx) self.m_Owner:GetTarget():AddDamage(self.damage) end");
        one.BaseData.Keywords.Remove("PresentationProbe");
        one.env["damage"] = new DamageInfo(one.Owner, one, two.Owner, two.Owner.GetHp() + 3);
        one.CallLua("Probe");
        var queued = scene.DrainPresentationEvents().Single();
        Check(queued.Applied!.IsPassive, "有效被动伤害请求计为一次生效");
        two.Owner.DealDamage(); two.Owner.DealFieldChange();
        var delayed = scene.DrainPresentationEvents();
        Check(delayed.Single(e => e.Damage != null).EffectId == queued.EffectId && delayed.Single(e => e.CardAdded != null).EffectId == queued.EffectId, "被动延迟伤害及伤口入包保留原生效ID");
        one.BaseData.ScanActivatable = false; one.BaseData.Keywords = ["Passive"];
        Script(one, "OnPassiveTick=function(self,ctx) self.m_Owner:ChangeShield(1) end");
        To(scene, 1);
        Check(scene.DrainPresentationEvents().Count(e => e.Card!.Id == one.Id && e.Applied?.Origin == "OnPassiveTick") == 1, "真实 Tick 直接被动回调覆盖");
        var triggerBuff = CardModifier.StatBuff("presentation-trigger", "Atk", 1);
        triggerBuff.ExpirePolicy = ExpirePolicy.TriggerBased; triggerBuff.RemoveTriggerId = "OnProbe";
        one.AddCardBuff(triggerBuff);
        scene.TriggerCombatEvent("OnProbe", new APIContext { Caster = one.Owner, SourceCard = one, Scene = scene });
        Check(scene.DrainPresentationEvents().Single().Applied!.Origin == "CardBuffTrigger:OnProbe", "卡直接事件回调有实际效果时归属卡实例");
        scene.Cleanup();
    }
    private static void BufferLifecycle()
    {
        var a = NewNpc(); var b = NewNpc(); var source = Place(a, "bp_focus", 1, 0);
        var scene = Scene(a, b); var card = Card(scene, a, source); Light(card);
        var first = scene.ReadPresentationEvents(); var repeated = scene.ReadPresentationEvents();
        Check(first.Count == repeated.Count && first[0].Sequence == repeated[0].Sequence, "重复读取不消费或增序号");
        var drained = scene.DrainPresentationEvents(); Check(drained.Count == first.Count && scene.ReadPresentationEvents().Count == 0, "转交释放缓冲");
        To(scene, 10); var last = scene.ReadPresentationEvents();
        Check(last.Count > 0 && last[0].Sequence > first[^1].Sequence, "转交后序号单调");
        scene.EnablePresentationRecording(false); To(scene, 60);
        Check(scene.ReadPresentationEvents().Count == 0, "关闭即释放，关闭期间不积压");
        scene.EnablePresentationRecording(true); Script(card, "Probe=function(self,ctx) self.m_Owner:ChangeShield(1) end"); card.CallLua("Probe");
        Check(scene.ReadPresentationEvents().Single().Sequence > last[^1].Sequence, "重新启用不复用序号");
        scene.Cleanup(); Check(scene.ReadPresentationEvents().Count == 0, "清理释放观察历史");
    }
}
