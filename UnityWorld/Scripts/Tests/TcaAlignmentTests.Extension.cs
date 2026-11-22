using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

public static partial class TcaAlignmentTests
{
    private static void VerifyTcaExtension(Lua lua)
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        foreach (var npc in new[] { a, b })
            npc.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-extension"));
        a.Stats.SetBase("ShenShi", 24);

        Card Place(CardDefine define, int x, int y)
        {
            var card = CardMgr.Instance.InstantiateFromDefine(define);
            a.GainCard(card);
            a.PlaceBackpackCardInternal(card.Id, x, y);
            return card;
        }

        var artifactData = Place(new CardDefine
        {
            ID = "bp_focus", Size = 2, Keywords = ["FaBao"],
            ManaCost = new() { ["Huo"] = 1, ["Mu"] = 1 }
        }, 2, 0);
        var neighborData = BackpackModelTests.Place(a, "bp_attack", 4, 0);
        var sliderData = BackpackModelTests.Place(a, "bp_attack", 3, 4);
        var observerData = Place(new CardDefine
        {
            ID = "tca_extension_observer", Size = 2, Keywords = ["Passive"]
        }, 2, 2);
        Place(new CardDefine { ID = "tca_extension_stop", Size = 1, Keywords = ["Passive"] }, 1, 3);
        var opponentCard = BackpackModelTests.Place(b, "bp_attack", 1, 0);

        var scene = new CombatScene();
        scene.Init(7491, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 1000);
        var events = new Dictionary<string, List<APIContext>>();
        var listeners = new List<(string Event, DelegateEventListener Listener)>();
        List<APIContext> Track(string name, Action<APIContext>? inspect = null)
        {
            var collected = new List<APIContext>();
            events.Add(name, collected);
            var listener = new DelegateEventListener((_, _, args) =>
            {
                var ctx = (APIContext)args;
                if (ctx.Scene != scene) return;
                collected.Add(ctx);
                inspect?.Invoke(ctx);
            });
            EventMgr.Instance.RegisterEvent("tca-extension-" + name, name, ScopeKey.Global, listener);
            listeners.Add((name, listener));
            return collected;
        }

        try
        {
            scene.PreStart();
            var owner = scene.GetCombatants()[a.Id];
            var enemy = scene.GetCombatants()[b.Id];
            CombatCard Find(Card card) => owner.GetField().Single(current => current.Id == card.Id);
            var artifact = Find(artifactData);
            var neighbor = Find(neighborData);
            var slider = Find(sliderData);
            var observer = Find(observerData);
            var other = enemy.GetField().Single(current => current.Id == opponentCard.Id);
            var artifactCtx = new APIContext { Caster = owner, SourceCard = artifact, Scene = scene };
            var sliderCtx = new APIContext { Caster = owner, SourceCard = slider, Scene = scene };
            var allyCtx = new APIContext { Caster = owner, SourceCard = observer, Scene = scene };

            observer.env = (LuaTable)lua.DoString("""
                return {OnManaCharged = function(self, ctx)
                    if ctx.Caster == self.m_Owner and CheckManaCharged(ctx, 'Huo', 'FaBao', false) then
                        GainShield(ctx, 'Self', 1)
                        AddElementBuff(ctx, 'Self', 'Tu', true, 0)
                    end
                end,
                OnWoundGained = function(self, ctx)
                    RemoveRandomWound(ctx, 'Self', 99, true)
                end}
                """)[0];
            observer.env["m_Owner"] = owner;
            observer.ScanLuaHooks();

            var watchedCell = new BackpackCell(1, 0);
            bool sourceWasFree = false;
            var manaIn = Track("OnManaCharged", ctx =>
            {
                if (ctx.SourceCard == artifact && ctx.GetStringValue("ChargedElement") == "Mu")
                    sourceWasFree = !owner.Backpack.Mana.ContainsKey(watchedCell);
            });
            var payment = Track("OnManaUsed");
            var activated = Track("OnArtifactActivated");
            var renewed = Track("OnArtifactRenewed");
            var deactivated = Track("OnArtifactDeactivated");
            var uses = Track("OnUse");
            using var grantAura = lua.GetFunction("GrantFreezeImmunityAura");
            using var thaw = lua.GetFunction("ThawCard");
            using var freeze = lua.GetFunction("Freeze");
            using var strip = lua.GetFunction("StripCardMana");
            using var checkPaid = lua.GetFunction("CheckManaUsed");
            grantAura.Call(artifactCtx, "Right", "FaShu");
            Check(neighbor.Freeze(), "光环未激活时邻卡可以被冻结");
            var neighborCtx = new APIContext { Caster = owner, SourceCard = neighbor, Scene = scene };
            thaw.Call(neighborCtx, "Self");
            Check(!neighbor.IsFrozen, "Lua 解冻立即清掉既有冻结");

            using var power = lua.GetFunction("AddArtifactPower");
            power.Call(artifactCtx, "Self", "Huo", 1, 0);
            scene.Start();
            Check(manaIn.Count == 1 && manaIn[0].SourceCard == artifact && manaIn[0].GetStringValue("ChargedElement") == "Huo" &&
                manaIn[0].GetStringValue("Element") == "Tu" && owner.ShieldValue == 1, "轮首供能经公开充入 Hook 驱动实卡 Lua 效果");
            owner.Backpack.PlaceMana(watchedCell, ElementType.Mu);
            owner.Backpack.Charge(scene.TicksPerRow);
            scene.Tick();
            Check(manaIn.Count == 2 && sourceWasFree && manaIn[1].GetValue("IsFullyCharged", false)
                && manaIn[1].GetValue("LitCount", 0) == 2, "扫描灵元先释放源格，再报告已充满");
            Check(activated.Count == 1 && renewed.Count == 0 && deactivated.Count == 0 && artifact.Phase == CombatCardPhase.Active,
                "法宝首次发动只产生激活");
            var firstUse = uses.Single(ctx => ctx.SourceCard == artifact);
            Check(payment[0].EventId < activated[0].EventId && activated[0].EventId < firstUse.EventId,
                "首次发动按照实付、法宝激活、OnUse 的顺序广播");
            Check(!scene.PresentationRecordingEnabled && payment.Count == 1 &&
                payment[0].GetValue("PaidHuo", 0) == 1 && payment[0].GetValue("PaidMu", 0) == 1 &&
                (bool)checkPaid.Call(payment[0], "Kinds", 2)[0] && !(bool)checkPaid.Call(payment[0], "Huo", 2)[0],
                "一次混色支付只发一次，关闭表现仍保留各五行实付数量");

            Check(!neighbor.Freeze(), "激活光环阻止直接冻结相邻目标");
            freeze.Call(allyCtx, "Random", 0);
            Check(allyCtx.CardTargets.Count == 1 && !allyCtx.CardTargets.Contains(neighbor) &&
                allyCtx.CardTargets[0].IsFrozen, "随机冻结先排除免疫卡再抽取");
            thaw.Call(allyCtx, "All");

            void Fill(BackpackCell cell, ElementType element)
            {
                owner.Backpack.PlaceMana(cell, element);
                Check(owner.Backpack.TryAssign(cell, artifact), "测试法宝实际收到指定灵元");
            }

            Fill(new(1, 1), ElementType.Huo);
            Fill(watchedCell, ElementType.Mu);
            strip.Call(artifactCtx, "Self", "Mu", 2);
            Check(artifactCtx.GetValue("RemovedCount", 0) == 1 && artifact.AssignedElements.SequenceEqual([ElementType.Huo])
                && !artifact.HasActivationResources() && !owner.Backpack.Mana.ContainsKey(watchedCell),
                "剥离实际木灵元，不返格或改变其余蓄灵");
            Fill(watchedCell, ElementType.Mu);
            owner.Backpack.Charge(scene.TicksPerRow * owner.Backpack.Height);
            scene.Tick();
            Check(activated.Count == 1 && renewed.Count == 1 && deactivated.Count == 0 && payment.Count == 2,
                "法宝续期独立于激活，成功续期也只提交一次支付");
            var secondUse = uses.Last(ctx => ctx.SourceCard == artifact);
            Check(payment[1].EventId < renewed[0].EventId && renewed[0].EventId < secondUse.EventId,
                "已激活法宝按照实付、法宝续期、OnUse 的顺序广播");

            owner.Backpack.Charge(scene.TicksPerRow * owner.Backpack.Height);
            scene.Tick();
            Check(deactivated.Count == 1 && artifact.Phase != CombatCardPhase.Active && payment.Count == 2 && neighbor.Freeze(),
                "缺灵失活、光环撤销、失败扫描保留蓄灵且不广播支付");
            thaw.Call(neighborCtx, "Self");

            // 已修正的平局发生在两张牌 Apply 前。
            var sliderApplyBefore = slider.ApplyCount;
            var otherApplyBefore = other.ApplyCount;
            bool firedBeforeApply = false;
            var contests = Track("OnContestOverflow", _ =>
                firedBeforeApply = slider.ApplyCount == sliderApplyBefore && other.ApplyCount == otherApplyBefore);
            using var deflection = lua.GetFunction("AddDeflection");
            deflection.Call(allyCtx, "Self", 1);
            owner.ResolveContest(new ContestData { SourceCard = slider, ContestType = ContestType.Block, ContestValue = 4 },
                new ContestData { SourceCard = other, ContestType = ContestType.Zhan, ContestValue = 5 });
            Check(contests.Count == 1 && firedBeforeApply, "平局在两张牌 Apply 前广播一次");
            var draw = contests[0];
            Check(draw.GetValue("IsDraw", false) && draw.GetValue("Overflow", -1) == 0 &&
                draw.GetValue("ContestValueA", 0) == 4 && draw.GetValue("ContestValueB", 0) == 4 &&
                draw.Get<CombatCard>("ContestCardA") == slider && draw.Get<CombatCard>("ContestCardB") == other,
                "5 点攻经卸力变 4 后与格挡打平，事件携带双方真实结果");

            // 伤势事件观察的是提交后的占格，重复请求和普通移除走原生命周期。
            var gained = Track("OnWoundGained", ctx =>
                Check(owner.Backpack.GetCardCells(ctx.SourceCard.Id).Count == ctx.GetValue("WoundSize", 0), "获得伤势时已经落格，体量不被动作参数覆盖"));
            var healed = Track("OnWoundHealed", ctx =>
                Check(owner.Backpack.GetCardCells(ctx.SourceCard.Id).Count == 0, "治愈伤势时背包已经释放"));
            var wound = owner.AddWound("bp_wound_wide");
            Check(gained.Count == 0, "新增伤势排队时没有提前派发");
            owner.DealFieldChange();
            using var removeWound = lua.GetFunction("RemoveRandomWound");
            using var checkWound = lua.GetFunction("CheckWoundEvent");
            removeWound.Call(allyCtx, "Self", 2, true);
            removeWound.Call(allyCtx, "Self", 2, true);
            Check(healed.Count == 0, "治愈排队时没有提前派发");
            owner.DealFieldChange();
            Check(gained.Count == 1 && healed.Count == 1 && healed[0].SourceCard == wound &&
                (bool)checkWound.Call(healed[0], owner, "Ally", 2)[0] &&
                !(bool)checkWound.Call(healed[0], enemy, "Ally", 2)[0], "治疗按实际离场一次，并保留所属阵营和体量");
            var ordinary = owner.AddWound("bp_wound_small");
            owner.DealFieldChange();
            owner.RemoveCombatCard(ordinary);
            owner.DealFieldChange();
            Check(gained.Count == 2 && healed.Count == 1, "普通伤势离场不被误报为治愈");

            VerifyElementTca(lua, owner, allyCtx, Track("OnElementStackChanged"));
            VerifyShieldTca(lua, owner, enemy, allyCtx, Track("OnShieldExhausted"), Track("OnShieldChargeReady"));

            var moves = Track("OnCardDisplaced");
            using var root = lua.GetFunction("RootCard");
            using var slide = lua.GetFunction("SlideCard");
            using var entered = lua.GetFunction("WasCardMovedIntoSide");
            root.Call(sliderCtx, "Self", 1);
            slide.Call(sliderCtx, "Self", "Up");
            Check(moves.Count == 0 && !sliderCtx.GetValue("Ret", true), "定身阻止滑移和位移广播");
            for (int i = 0; i < owner.Backpack.Height; i++) owner.Backpack.FinishRow();
            slide.Call(sliderCtx, "Self", "Up");
            slide.Call(sliderCtx, "Self", "Left");
            Check(moves.Count == 2 && (bool)entered.Call(moves[0], observer, "Down", "FaShu")[0] &&
                !(bool)entered.Call(moves[1], observer, "Down", "FaShu")[0],
                "定身跨轮结束，滑入下邻时匹配，沿相同方位滑动继续共边时不匹配");

            // 活跃法宝离场走同一个状态边沿，即使已离开 Field 也会广播给其他卡。
            Fill(watchedCell, ElementType.Mu);
            owner.Backpack.Charge(scene.TicksPerRow * owner.Backpack.Height);
            scene.Tick();
            Check(activated.Count == 2 && artifact.Phase == CombatCardPhase.Active, "已失活法宝恢复满灵后可再次激活");
            owner.RemoveCombatCard(artifact);
            owner.DealFieldChange();
            Check(deactivated.Count == 2 && deactivated[^1].SourceCard == artifact, "激活中的法宝实际离场触发失活");
        }
        finally
        {
            foreach (var (name, listener) in listeners)
                EventMgr.Instance.RemoveEvent(name, ScopeKey.Global, listener);
            scene.Cleanup();
        }
    }

    private static void VerifyElementTca(Lua lua, CombatNpc owner, APIContext ctx, List<APIContext> changes)
    {
        using var immune = lua.GetFunction("AddElementImmunity");
        using var add = lua.GetFunction("AddElementBuff");
        using var remove = lua.GetFunction("RemoveElementBuff");
        using var match = lua.GetFunction("CheckElementStackChange");
        const string fire = "Element_Debuff_Huo";
        owner.AddModifier(fire, 1);
        immune.Call(ctx, "Self", fire, 1);
        add.Call(ctx, "Self", "Huo", true, 2);
        Check(changes.Count == 2 && changes[0].GetValue("Before", -1L) == 0 && changes[1].GetValue("After", -1L) == 2 &&
            owner.GetAllModifiers().All(modifier => modifier.DefineId != "ElementImmunity_" + fire), "一次免疫拦一次施加，保留已有层");
        Check((bool)match.Call(changes[1], fire, "Increase")[0] &&
            !(bool)match.Call(changes[1], "Element_Debuff_Mu", "Increase")[0], "层数条件只匹配指定五行状态");
        remove.Call(ctx, "Self", "Huo", true, 2);
        Check(changes.Count == 4 && changes[2].GetValue("After", -1L) == 1 &&
            changes[3].GetValue("After", -1L) == 0, "清到零触发一次，实际移除无重复通知");
        owner.AddModifier(fire, 100);
        owner.AddModifier(fire, 1);
        Check(changes.Count == 5 && changes[^1].GetValue("After", 0L) == 99, "达到五行状态上限后不产生虚假变化");
        owner.RemoveModifier(fire);
        Check(changes.Count == 6 && changes[^1].GetValue("Before", 0L) == 99 &&
            changes[^1].GetValue("After", -1L) == 0, "主动移除非零状态产生真实层数变化");

        var define = CombatNpcModifierDefineMgr.Instance.Get(fire);
        var removeTrigger = define.RemoveTriggerId;
        try
        {
            define.RemoveTriggerId = "OnRoundEnd";
            owner.AddModifier(fire, 1);
            owner.Scene.TriggerCombatEvent("OnRoundEnd", ctx);
            Check(changes.Count == 8 && changes[^1].GetValue("Before", 0L) == 1 &&
                changes[^1].GetValue("After", -1L) == 0, "按事件扣掉最后一层仍准确报告一次");
        }
        finally
        {
            define.RemoveTriggerId = removeTrigger;
            owner.RemoveModifier(fire);
        }
    }

    private static void VerifyShieldTca(Lua lua, CombatNpc owner, CombatNpc enemy, APIContext ctx,
        List<APIContext> exhausted, List<APIContext> charged)
    {
        using var charge = lua.GetFunction("AddShieldCharge");
        using var breakShield = lua.GetFunction("ArmorBreak");
        using var checkReason = lua.GetFunction("CheckShieldExhaustion");
        owner.ChangeShield(-owner.ShieldValue);
        exhausted.Clear();
        charge.Call(ctx, "Self", "Huo", 3);
        owner.ChangeShield(5);
        int hp = owner.GetHp();
        void Hit(int damage, ElementType element) => owner.ApplyDamage(new DamageInfo(enemy, null, owner, damage, elementType: element));
        Hit(1, ElementType.Mu);
        Hit(1, ElementType.Huo);
        Check(charged.Count == 0, "异五行吸收不计入火系蓄力");
        Hit(2, ElementType.Huo);
        Check(charged.Count == 1 && charged[0].GetValue("Absorbed", 0L) == 3 && owner.GetHp() == hp,
            "两次完全被挡住的火伤累计达标，实际扣血为零");
        Hit(1, ElementType.Huo);
        Check(exhausted.Count == 1 && charged.Count == 1 && owner.ShieldValue == 0 &&
            (bool)checkReason.Call(exhausted[0], "Damage")[0], "本轮蓄力只兑现一次，恰好耗尽灵盾仍发事件");
        owner.ChangeShield(3);
        breakShield.Call(ctx, "Self", 3);
        Check(exhausted.Count == 2 && charged.Count == 1 && (bool)checkReason.Call(exhausted[1], "Effect")[0],
            "破盾效果报告自身原因，不计吸收");
        for (int row = 0; row < owner.Backpack.Height; row++) owner.Backpack.FinishRow();
        owner.ChangeShield(3);
        Hit(5, ElementType.Huo);
        Check(charged.Count == 2 && exhausted.Count == 3 && owner.GetHp() == hp - 2 &&
            charged[1].GetValue("Absorbed", 0L) == 3, "新轮重新蓄力，只累计实际吸收且保留余伤");
    }
}
