using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

public static partial class PresentationPlayerChecks
{
    // 初始化直接展示快照，空 Tick 立即提交普通状态。
    private static void InitialAndEmptyFramesDisplayImmediately(CheckContext checks)
    {
        var player = new BattlePresentationPlayer();
        var initial = Snapshot(0);
        var add = Fact(CombatPresentationKind.CardAddedToBackpack, 1, 10, Card(101)) with
        { Tick = 0, IsInitialization = true, CardAdded = new(Array.Empty<PresentationManaCell>()) };
        player.Submit(Frame(initial, 0, new[] { add }, initialization: true));
        checks.Check(player.IsIdle && player.StartedCueCount == 0, "initial cards do not fly into backpack");
        checks.Same(initial, player.DisplaySnapshot, "initial snapshot direct display");
        var next = Change(Snapshot(1), 10, n => n with { Mp = 19 });
        player.Submit(Frame(next, 1));
        checks.Check(player.IsIdle && player.DisplaySnapshot.Tick == 1, "empty Tick is delivered without animation");
        checks.Same(next, player.DisplaySnapshot, "empty Tick commits all ordinary state");
    }

    // 同卡灵元逐个到达，全部到达后才播放使用动画。
    private static void SameCardAbsorptionPrecedesUse(CheckContext checks)
    {
        var player = Start();
        var callbacks = new List<string>();
        player.CueStarted += c => callbacks.Add("S" + c.Event.Sequence);
        player.CueCompleted += c => callbacks.Add("E" + c.Event.Sequence);
        var first = Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" });
        var second = Mana(2, Card(101), 10, 4, new[] { "Shui" }, new[] { "Shui", "Shui" });
        var used = Fact(CombatPresentationKind.CardUsed, 3, 10, Card(101)) with
        { CardUsed = new(new[] { "Shui", "Shui" }, Array.Empty<string>()) };
        var end = Change(Snapshot(1), 10, n => n with
        {
            Cards = n.Cards.Select(c => c.Id == 101 ? c with { UseCount = 1 } : c).ToArray(),
            Backpack = n.Backpack with { Mana = Array.Empty<ManaSnapshot>() }
        });
        player.Submit(Frame(end, 0, new[] { first, second, used }));
        checks.Check(Visible(player, 101).AssignedElements.Count == 0, "resource is not lit before arrival");
        checks.Check(player.DisplaySnapshot.Participants[0].Backpack.Mana.Any(m => m.X == 4), "later resource stays at source");
        checks.Check(player.ActiveCues.Count == 1, "same card resource arrivals serialize");
        player.Update(.181);
        checks.Check(Visible(player, 101).AssignedElements.Count == 1, "first arrival lights half of size two card");
        player.Update(.18);
        checks.Check(Visible(player, 101).AssignedElements.Count == 2, "full light remains during use flash");
        checks.Check(player.ActiveCues.Single().Event.Kind == CombatPresentationKind.CardUsed, "use waits for both arrivals");
        checks.Drain(player);
        checks.Same(end, player.DisplaySnapshot, "use returns to final zero light state");
        checks.Check(string.Join(",", callbacks) == "S1,E1,S2,E2,S3,E3", "same Tick absorption and use preserve causal order");
    }

    // 不同卡牌并行播放，并共享相同的显示时间。
    private static void UnrelatedCardsAnimateInParallel(CheckContext checks)
    {
        var player = Start();
        player.Submit(Frame(Snapshot(1), 0, new[]
        {
            Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" }),
            Mana(2, Card(201), 20, 3, Array.Empty<string>(), new[] { "Shui" })
        }));
        checks.Check(player.ActiveCues.Count == 2, "unrelated cards animate in parallel");
        player.Update(.09);
        checks.Check(player.ActiveCues.All(c => Math.Abs(c.Progress - .5f) < .001), "parallel cards share display time");
        checks.Drain(player);
    }

    // 同 Tick 对撞使用历史双槽，新待发卡不会被旧对撞覆盖。
    private static void HistoricalCollisionPreservesReplacement(CheckContext checks)
    {
        var player = Start();
        var replacement = Card(102);
        var end = Change(Snapshot(1), 10, n => n with { Pending = Pending(replacement) });
        var facts = new[]
        {
            PendingFact(1, 10, Card(101), true), PendingFact(2, 20, Card(201), true),
            PendingFact(3, 10, Card(101), false), PendingFact(4, 20, Card(201), false),
            Contest(5), PendingFact(6, 10, replacement, true)
        };
        player.Submit(Frame(end, 0, facts));
        checks.Check(player.DisplaySnapshot.Participants[0].Pending?.CardId == 101 &&
              player.DisplaySnapshot.Participants[1].Pending?.CardId == 201,
            "same Tick removed slots reappear as actual historical collision pair");
        checks.Check(player.ActiveCues.Single().Event.Kind == CombatPresentationKind.ContestResolved, "one real contest cue");
        player.Update(.1);
        checks.Check(player.DisplaySnapshot.Participants[0].Pending?.CardId == 101, "replacement does not overwrite unfinished collision");
        checks.Drain(player);
        checks.Same(end, player.DisplaySnapshot, "new pending card survives old collision completion");
    }

    // 平局保留真实对撞，直接命中不凭空生成对手槽。
    private static void DrawAndDirectHitUseActualParticipants(CheckContext checks)
    {
        var player = Start();
        player.Submit(Frame(Snapshot(1), 0, new[] { Contest(1) with
        { Contest = Contest(1).Contest! with { WinnerNpcId = null, Difference = 0 } } }));
        checks.Check(player.ActiveCues.Single().Event.Contest!.WinnerNpcId == null, "draw retains a two card collision");
        checks.Drain(player);
        var straight = Start();
        straight.Submit(Frame(Snapshot(1), 0, new[]
        { PendingFact(1, 10, Card(101), true), PendingFact(2, 10, Card(101), false) }));
        checks.Check(straight.ActiveCues.All(c => c.Event.Kind != CombatPresentationKind.ContestResolved), "direct hit invents no collision");
        checks.Check(straight.DisplaySnapshot.Participants[1].Pending == null, "direct hit invents no opponent slot");
        checks.Drain(straight);
    }

    // 大步长与小步长保持同样的回调顺序和结果，且不修改输入。
    private static void LargeDeltaMatchesSmallStepsWithoutMutatingInput(CheckContext checks)
    {
        var facts = new[]
        {
            Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" }),
            Mana(2, Card(201), 20, 3, Array.Empty<string>(), new[] { "Shui" }),
            Fact(CombatPresentationKind.CardUsed, 3, 10, Card(101)) with
                { CardUsed = new(new[] { "Shui" }, Array.Empty<string>()) },
            Contest(4), Damage(5) with { ContestId = 700 },
            Fact(CombatPresentationKind.CardApplied, 6, 10, Card(101)) with
                { ContestId = 700, Applied = new("Apply", false) }
        };
        var frame = Frame(Snapshot(1), 0, facts);
        var input = System.Text.Json.JsonSerializer.Serialize(frame, BattleJson.Options);
        (BattlePresentationPlayer Player, List<string> Trace) Create()
        {
            var p = Start(); var trace = new List<string>();
            p.CueStarted += c => trace.Add("S" + c.Event.Sequence);
            p.CueCompleted += c => trace.Add("E" + c.Event.Sequence);
            p.Submit(frame);
            p.Submit(Frame(Snapshot(2), 6, new[] { Damage(7) with { Tick = 2 } }));
            return (p, trace);
        }
        var small = Create(); var large = Create();
        for (var i = 0; i < 10000 && !small.Player.IsIdle; i++) small.Player.Update(.01);
        checks.Drain(large.Player);
        checks.Check(small.Player.IsIdle, "small delta playback converges");
        checks.Check(string.Join(",", small.Trace) == string.Join(",", large.Trace), "large delta preserves every start and completion boundary order");
        checks.Same(small.Player.DisplaySnapshot, large.Player.DisplaySnapshot, "large delta final state matches small delta");
        checks.Check(input == System.Text.Json.JsonSerializer.Serialize(frame, BattleJson.Options), "reducer never mutates submitted facts or snapshots");
    }

    // 跨 Tick 的名字、伤害、吸收和对撞可以重叠，不阻塞新扫描进度。
    private static void CrossTickAnimationsOverlapWithoutBlockingProgress(CheckContext checks)
    {
        var player = Start();
        var name = Fact(CombatPresentationKind.CardApplied, 1, 10, Card(101)) with
            { ActionId = 55, EffectId = 77, Applied = new("Apply", false) };
        player.Submit(Frame(Snapshot(1), 0, new[] { name, Damage(2) with { Card = Card(101), ActionId = 55, EffectId = 77 } }));
        checks.Check(player.ActiveCues.Count == 2, "name and related damage appear together instead of serial full fades");
        player.Update(.05);
        var next = Change(Snapshot(2), 10, n => n with { Mp = 9, Backpack = n.Backpack with { Row = 2, ScanProgress = 7 } });
        player.Submit(Frame(next, 2, new[]
        {
            Mana(3, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" }) with { Tick = 2 },
            Fact(CombatPresentationKind.CardUsed, 4, 10, Card(101)) with
                { Tick = 2, CardUsed = new(new[] { "Shui" }, Array.Empty<string>()) },
            Mana(5, Card(201), 20, 3, Array.Empty<string>(), new[] { "Shui" }) with { Tick = 2 }
        }));
        checks.Check(player.ActiveCues.Count(c => c.Event.ManaAssigned != null) == 2 &&
              player.ActiveCues.Count(c => c.Event.Applied != null || c.Event.Damage != null) == 2,
            "next Tick both actors absorb while prior name and damage are still fading");
        checks.Check(player.DisplaySnapshot.Tick == 2 && player.DisplaySnapshot.Participants[0].Backpack.Row == 2 &&
              player.DisplaySnapshot.Participants[0].Backpack.ScanProgress == 7 && player.DisplaySnapshot.Participants[0].Mp == 9,
            "new Tick and scan progress are visible before old animations finish");
        player.Update(.19);
        checks.Check(player.ActiveCues.Any(c => c.Event.CardUsed != null) && player.ActiveCues.Any(c => c.Event.Applied != null),
            "same-card use begins without waiting for old name fade");
        player.Submit(Frame(Snapshot(3), 5, new[] { Contest(6) with { Tick = 3 } }));
        player.Update(.11);
        checks.Check(player.ActiveCues.Any(c => c.Event.Contest != null) && player.ActiveCues.Any(c => c.Event.Applied != null),
            "next Tick collision overlaps old name and damage tails");
        checks.Drain(player);
        checks.Same(Snapshot(3), player.DisplaySnapshot, "cross Tick overlapping actors settle latest snapshot");
    }
}
