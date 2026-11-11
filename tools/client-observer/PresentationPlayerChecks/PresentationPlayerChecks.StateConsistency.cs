using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

public static partial class PresentationPlayerChecks
{
    // 新增卡牌只保留一个显示身份，插入完成才释放依赖事件。
    private static void CardInsertionPrecedesDependentEvents(CheckContext checks)
    {
        var player = Start();
        var wound = Card(301) with { Name = "伤口", Keywords = new[] { "Wound" }, Cells = new[] { new PresentationCell(3, 0) } };
        var added = Fact(CombatPresentationKind.CardAddedToBackpack, 1, 10, wound) with
        { CardAdded = new(new[] { new PresentationManaCell(new(3, 0), "Shui") }) };
        var mana = Mana(2, wound, 10, 4, Array.Empty<string>(), new[] { "Shui" });
        var final = Change(Snapshot(1), 10, n => n with
        { Cards = n.Cards.Append(BattlePresentationState.Copy(wound, 0) with { AssignedElements = new[] { "Shui" } }).ToArray() });
        player.Submit(Frame(final, 0, new[] { added, mana }));
        checks.Check(player.DisplaySnapshot.Participants[0].Cards.Count(c => c.Id == 301) == 1, "new card has one display identity");
        checks.Check(player.ActiveCues.Single().Event.CardAdded != null, "active add cue supplies static-card hiding lifetime");
        checks.Check(player.DisplaySnapshot.Participants[0].Backpack.Mana.All(m => m.X != 3), "replaced mana disappears without fake mana-flight cue");
        player.Update(.401);
        checks.Check(player.ActiveCues.Single().Event.ManaAssigned != null, "subsequent new-card event waits for insertion animation");
        checks.Check(player.ActiveCues.All(c => c.Event.CardAdded == null), "insertion end releases static-card hiding");
        checks.Drain(player);
        checks.Same(final, player.DisplaySnapshot, "new card settles actual snapshot");
        var removed = Snapshot(2);
        player.Submit(Frame(removed, 2));
        checks.Check(player.DisplaySnapshot.Participants[0].Cards.All(c => c.Id != 301), "later removal is retained after insertion callbacks");
        checks.Same(removed, player.DisplaySnapshot, "later card removal final consistency");
    }

    // 同 Tick 新增又移除时先展示真实插入，最终仍以移除为准。
    private static void SameTickRemovalWinsAfterInsertion(CheckContext checks)
    {
        var player = Start();
        var card = Card(302);
        player.Submit(Frame(Snapshot(1), 0, new[]
        { Fact(CombatPresentationKind.CardAddedToBackpack, 1, 10, card) with { CardAdded = new(Array.Empty<PresentationManaCell>()) } }));
        checks.Check(player.DisplaySnapshot.Participants[0].Cards.Any(c => c.Id == 302), "same Tick added-then-removed card still shows real insertion");
        checks.Drain(player);
        checks.Check(player.DisplaySnapshot.Participants[0].Cards.All(c => c.Id != 302), "same Tick removal wins after insertion settles");
        var failed = Start();
        failed.Submit(Frame(Snapshot(1), 0));
        checks.Check(failed.StartedCueCount == 0, "failed insertion with no success fact invents no animation");
    }

    // 伤害动画等待父动作时，普通属性和权威血量仍立即更新。
    private static void OrdinaryStateUpdatesWhileDamageWaits(CheckContext checks)
    {
        var player = Start();
        var final = Change(Change(Snapshot(1), 10, n => n with { Mp = 7, Sp = 12, HpMax = 444 }),
            20, n => n with { Hp = 80, Mp = 11, SpMax = 17 });
        var resource = Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" }) with { ActionId = 90 };
        var damage = Damage(2) with { ParentActionId = 90 };
        player.Submit(Frame(final, 0, new[] { resource, damage }));
        var left = player.DisplaySnapshot.Participants[0]; var right = player.DisplaySnapshot.Participants[1];
        checks.Check(left.Mp == 7 && left.Sp == 12 && left.HpMax == 444 && right.Mp == 11 && right.SpMax == 17,
            "ordinary values untouched by events update immediately");
        checks.Check(right.Hp == 80, "current HP is authoritative even while its historical damage waits to appear");
        checks.Check(player.ActiveCues.Single().Event.Kind == CombatPresentationKind.ManaAssigned, "damage waits for parent action");
        checks.Drain(player);
        checks.Same(final, player.DisplaySnapshot, "mixed ordinary and animated state final correctness");
    }

    // 被动效果即使先记录，也要等卡牌插入完成；事实顺序不变。
    private static void PassiveEffectWaitsForItsCardInsertion(CheckContext checks)
    {
        var player = Start();
        var newCard = Card(303) with { Name = "狼牙", Keywords = new[] { "Passive" } };
        var producer = Fact(CombatPresentationKind.CardApplied, 1, 10, Card(101)) with
            { ActionId = 55, EffectId = 77, Applied = new("Apply", false) };
        var passive = Fact(CombatPresentationKind.CardApplied, 2, 10, newCard) with
            { ParentActionId = 55, EffectId = 88, Applied = new("Apply", true) };
        var addition = Fact(CombatPresentationKind.CardAddedToBackpack, 3, 10, newCard) with
            { ActionId = 55, EffectId = 77, CardAdded = new(Array.Empty<PresentationManaCell>()) };
        var final = Change(Snapshot(1), 10, n => n with
            { Cards = n.Cards.Append(BattlePresentationState.Copy(newCard, 0)).ToArray() });
        var events = new[] { producer, passive, addition };
        var callbacks = new List<string>();
        player.CueStarted += c => callbacks.Add("S" + c.Event.Sequence);
        player.CueCompleted += c => callbacks.Add("E" + c.Event.Sequence);
        player.Submit(Frame(final, 0, events));
        checks.Check(callbacks.SequenceEqual(new[] { "S1", "S3" }), "new insertion overlaps the existing generator name fade");
        checks.Drain(player);
        checks.Check(callbacks.SequenceEqual(new[] { "S1", "S3", "E3", "S2", "E1", "E2" }),
            "new passive card insertion completes before its earlier recorded Start effect is displayed");
        checks.Check(events.Select(e => e.Sequence).SequenceEqual(new long[] { 1, 2, 3 }), "playback plan never reorders authoritative facts");
        checks.Check(player.CompletedCueCount == 3, "shared creator Action and Effect locks do not form a cycle");
        checks.Same(final, player.DisplaySnapshot, "passive-before-add fact sequence settles complete snapshot");
    }

    // 同卡跨 Tick 保持点亮与使用顺序，旧到达不能回退全局 Tick。
    private static void CrossTickCardChainPreservesLatestTick(CheckContext checks)
    {
        var player = Start();
        var one = Change(Snapshot(1), 10, n => n with
            { Cards = n.Cards.Select(c => c with { AssignedElements = new[] { "Shui" } }).ToArray() });
        player.Submit(Frame(one, 0, new[] { Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" }) }));
        player.Update(.05);
        var two = Change(Snapshot(2), 10, n => n with { Cards = n.Cards.Select(c => c with { UseCount = 1 }).ToArray() });
        player.Submit(Frame(two, 1, new[]
        {
            Mana(2, Card(101), 10, 4, new[] { "Shui" }, new[] { "Shui", "Shui" }) with { Tick = 2 },
            Fact(CombatPresentationKind.CardUsed, 3, 10, Card(101)) with
                { Tick = 2, CardUsed = new(new[] { "Shui", "Shui" }, Array.Empty<string>()) }
        }));
        checks.Check(player.DisplaySnapshot.Tick == 2 && player.ActiveCues.Count == 1, "only same-card transition waits across Ticks");
        player.Update(.131);
        checks.Check(Visible(player, 101).AssignedElements.Count == 1 && player.DisplaySnapshot.Tick == 2,
            "old Tick arrival updates only local light and never rolls global Tick back");
        player.Update(.18);
        checks.Check(Visible(player, 101).AssignedElements.Count == 2 && player.ActiveCues.Single().Event.CardUsed != null,
            "cross Tick full light is preserved until use flash completes");
        checks.Drain(player);
        checks.Same(two, player.DisplaySnapshot, "local cross Tick chain finishes at newest authoritative card state");
    }

    // 同角色多组文字可并行，旧文字完成不能回写旧血量。
    private static void ConcurrentHeadTextPreservesLatestHp(CheckContext checks)
    {
        var player = Start();
        CombatPresentationEvent Name(long sequence, int tick) => Fact(CombatPresentationKind.CardApplied, sequence, 20, Card(201)) with
            { Tick = tick, ActionId = 22, EffectId = sequence, Applied = new("Apply", false) };
        var first = Change(Snapshot(1), 20, n => n with { Hp = 80 });
        player.Submit(Frame(first, 0, new[] { Name(1, 1), Damage(2) with { Card = Card(201), ActionId = 22 } }));
        player.Update(.2);
        var second = Change(Snapshot(2), 20, n => n with { Hp = 60 });
        player.Submit(Frame(second, 2, new[]
        {
            Name(3, 2), Damage(4) with { Tick = 2, Card = Card(201), ActionId = 22,
                Damage = Damage(4).Damage! with { HpBefore = 80, HpAfter = 60 } }
        }));
        checks.Check(player.ActiveCues.Count(c => c.Event.Applied != null) == 2 && player.ActiveCues.Count(c => c.Event.Damage != null) == 2,
            "same actor can display two names and two damage numbers concurrently");
        checks.Check(player.DisplaySnapshot.Participants[1].Hp == 60, "concurrent damage uses latest HP immediately");
        player.Update(.61);
        checks.Check(player.DisplaySnapshot.Tick == 2 && player.DisplaySnapshot.Participants[1].Hp == 60 && player.ActiveCues.Count == 2,
            "old text completion cannot restore old HP or finish new texts");
        checks.Drain(player);
        checks.Same(second, player.DisplaySnapshot, "concurrent head text final state is latest");
    }

    // 空 Tick 的新卡面、护盾、治疗和待发槽不被旧文字完成覆盖。
    private static void OldHeadTextCannotOverwriteNewSnapshot(CheckContext checks)
    {
        var player = Start();
        player.Submit(Frame(Snapshot(1), 0, new[] { Damage(1), Fact(CombatPresentationKind.CardApplied, 2, 10, Card(101)) with
            { Applied = new("Apply", false) } }));
        player.Update(.1);
        var latest = Change(Change(Snapshot(2), 20, n => n with { Hp = 95, Shield = 12 }), 10, n => n with
        {
            Pending = Pending(Card(102)),
            Cards = n.Cards.Select(c => c with { Name = "最新卡面", AssignedElements = new[] { "Shui", "Shui" } }).ToArray()
        });
        player.Submit(Frame(latest, 2));
        checks.Same(latest, player.DisplaySnapshot, "empty Tick state updates while old head text is alive");
        checks.Drain(player);
        checks.Same(latest, player.DisplaySnapshot, "late old text cannot overwrite newer cards shield healing or slots");
    }

    // 跨 Tick 对撞保留占用槽，结束后交给新待发卡而不误清空。
    private static void OldCollisionReleasesSlotToReplacement(CheckContext checks)
    {
        var player = Start();
        player.Submit(Frame(Snapshot(1), 0, new[] { Contest(1) }));
        player.Update(.1);
        var next = Change(Snapshot(2), 10, n => n with { Pending = Pending(Card(102)) });
        player.Submit(Frame(next, 1, new[] { PendingFact(2, 10, Card(102), true) with { Tick = 2 } }));
        checks.Check(player.DisplaySnapshot.Tick == 2 && player.DisplaySnapshot.Participants[0].Pending?.CardId == 101,
            "only occupied slot retains historical collision while new Tick is visible");
        player.Update(.16);
        checks.Check(player.DisplaySnapshot.Participants[0].Pending?.CardId == 102 && player.IsIdle,
            "old collision completion releases slot to newer pending card");
        checks.Same(next, player.DisplaySnapshot, "cross Tick collision cannot clear replacement slot");
    }

    // 新 Tick 已移除卡牌时，迟到的插入动画不能让它复活。
    private static void OldInsertionCannotResurrectRemovedCard(CheckContext checks)
    {
        var player = Start();
        var newCard = Card(304);
        var added = Change(Snapshot(1), 10, n => n with { Cards = n.Cards.Append(BattlePresentationState.Copy(newCard, 0)).ToArray() });
        player.Submit(Frame(added, 0, new[] { Fact(CombatPresentationKind.CardAddedToBackpack, 1, 10, newCard) with
            { CardAdded = new(Array.Empty<PresentationManaCell>()) } }));
        player.Update(.1);
        player.Submit(Frame(Snapshot(2), 1));
        checks.Check(player.DisplaySnapshot.Tick == 2 && player.ActiveCues.Any(c => c.Event.CardAdded != null),
            "late insertion may finish locally after newer removal without holding the Tick");
        checks.Drain(player);
        checks.Check(player.DisplaySnapshot.Participants[0].Cards.All(c => c.Id != 304), "old insertion completion never resurrects removed card");
        checks.Same(Snapshot(2), player.DisplaySnapshot, "cross Tick card removal final consistency");
    }

    // 旧灵元飞行动画不能移除或覆盖新 Tick 在同格生成的资源。
    private static void OldManaFlightPreservesReusedCell(CheckContext checks)
    {
        var initial = Change(Snapshot(0), 10, n => n with
            { Cards = n.Cards.Select(c => c with { AssignedElements = new[] { "Shui", "Shui" } }).ToArray() });
        var player = Start(initial);
        var first = Change(Snapshot(1), 10, n => n with
        {
            Cards = n.Cards.Select(c => c with { UseCount = 1, AssignedElements = new[] { "Shui" } }).ToArray(),
            Backpack = n.Backpack with { Mana = Array.Empty<ManaSnapshot>() }
        });
        player.Submit(Frame(first, 0, new[]
        {
            Fact(CombatPresentationKind.CardUsed, 1, 10, Card(101)) with
                { CardUsed = new(new[] { "Shui", "Shui" }, Array.Empty<string>()) },
            Mana(2, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" })
        }));
        player.Update(.05);
        var latest = Change(first with { Tick = 2 }, 10, n => n with
            { Backpack = n.Backpack with { Mana = new[] { new ManaSnapshot(3, 0, "Huo") } } });
        player.Submit(Frame(latest, 2));
        checks.Check(player.DisplaySnapshot.Participants[0].Backpack.Mana.Single().Element == "Huo",
            "new Tick resource at reused cell appears before old delayed flight starts");
        player.Update(.06);
        checks.Check(player.ActiveCues.Single().Event.ManaAssigned != null &&
              player.DisplaySnapshot.Participants[0].Backpack.Mana.Single().Element == "Huo",
            "old flight start cannot remove a new resource that reused the same cell");
        checks.Check(Visible(player, 101).AssignedElements.Count == 0, "old use only resets its local chain before new resource arrival");
        checks.Drain(player);
        checks.Same(latest, player.DisplaySnapshot, "old flight completion preserves newer resource cell and latest card charge");
    }
}
