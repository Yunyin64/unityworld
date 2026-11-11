using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

public static partial class PresentationPlayerChecks
{
    // 重复帧不重复播放；后续 Tick 可以并行，但每帧只结算一次。
    private static void DuplicateFramesSettleOnlyOnce(CheckContext checks)
    {
        var player = Start();
        var settled = new List<int>();
        player.SnapshotSettled += s => settled.Add(s.Tick);
        var first = Frame(Snapshot(1), 0, new[] { Damage(1) });
        player.Submit(first);
        player.Submit(first);
        player.Submit(Frame(Snapshot(2), 1));
        player.Submit(Frame(Snapshot(3), 1, new[] { Damage(2) with { Tick = 3 } }));
        checks.Check(player.StartedCueCount == 2 && player.ActiveCues.Count == 2,
            "duplicate frame is ignored while later Tick damage starts concurrently");
        checks.Drain(player);
        checks.Check(settled.Order().SequenceEqual(new[] { 1, 2, 3 }), "each Tick settles once; independent empty Tick can finish first");
        checks.Check(player.StartedCueCount == 2 && player.CompletedCueCount == 2, "each damage appears exactly once");
        checks.Same(Snapshot(3), player.DisplaySnapshot, "multiple frame final state");
    }

    // 缺 Tick、未知版本与 Tick 不匹配均有诊断，并按快照恢复。
    private static void InvalidFramesRecoverFromCompleteSnapshot(CheckContext checks)
    {
        var gap = Start();
        gap.Submit(Frame(Snapshot(2), 0));
        checks.Check(gap.Diagnostic?.Contains("缺失") == true && gap.IsIdle, "Tick gap explicitly recovers");
        checks.Same(Snapshot(2), gap.DisplaySnapshot, "Tick gap uses complete latest snapshot");
        gap.Submit(Frame(Snapshot(3), 0, new[] { Damage(1) with { Tick = 3 } }));
        checks.Drain(gap);
        checks.Check(gap.CompletedCueCount == 1, "playback resumes after gap recovery");
        var version = Start();
        version.Submit(Frame(Snapshot(1), 0) with { SchemaVersion = 999 });
        checks.Check(version.Diagnostic?.Contains("版本") == true && version.IsIdle, "unknown version explicitly recovers");
        checks.Same(Snapshot(1), version.DisplaySnapshot, "version recovery final snapshot");
        var wrongTick = Start();
        wrongTick.Submit(Frame(Snapshot(2), 0) with { Tick = 1 });
        checks.Check(wrongTick.Diagnostic?.Contains("Tick") == true, "frame and snapshot Tick mismatch is diagnosed");
    }

    // 跨 Tick 重复事件和非法序号范围不能进入动画队列。
    private static void InvalidEventSequencesAreRejected(CheckContext checks)
    {
        var player = Start();
        player.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }));
        checks.Drain(player);
        player.Submit(Frame(Snapshot(2), 1, new[] { Damage(1) with { Tick = 2 } }));
        checks.Check(player.Diagnostic?.Contains("序号") == true, "duplicate event in new Tick is diagnosed");
        checks.Check(player.StartedCueCount == 1 && player.IsIdle, "duplicate event never becomes a cue");
        var range = Start();
        range.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }) with { FirstSequence = 50 });
        checks.Check(range.Diagnostic?.Contains("范围") == true, "invalid event range is diagnosed");
    }

    // 单批事件超限时直接恢复完整快照，不扩张动画队列。
    private static void OversizedBatchSkipsAnimations(CheckContext checks)
    {
        var player = Start();
        player.MaxQueuedEvents = 1;
        var final = Snapshot(1);
        player.Submit(Frame(final, 0, new[] { Damage(1), Damage(2) }));
        checks.Check(player.Diagnostic?.Contains("已跳过部分动画") == true, "event overflow is visibly diagnosed");
        checks.Check(player.IsIdle && player.ActiveCues.Count == 0 && player.StartedCueCount == 0, "oversized batch does not grow active queue");
        checks.Same(final, player.DisplaySnapshot, "event overflow recovers latest complete state");
    }

    // 累计事件超限取消旧动画，后续从新消费游标恢复。
    private static void EventOverflowCancelsOldGeneration(CheckContext checks)
    {
        var player = Start();
        player.MaxQueuedEvents = 2;
        player.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }));
        var generation = player.Generation;
        player.Submit(Frame(Snapshot(2), 1, new[] { Damage(2) with { Tick = 2 } }));
        player.Submit(Frame(Snapshot(3), 2, new[] { Damage(3) with { Tick = 3 } }));
        checks.Check(player.Generation > generation && player.IsIdle, "overflow cancels active and pending generations");
        var completed = player.CompletedCueCount;
        player.Update(100);
        checks.Check(player.CompletedCueCount == completed, "canceled overflow cues cannot complete later");
        player.Submit(Frame(Snapshot(4), 3, new[] { Damage(4) with { Tick = 4 } }));
        checks.Drain(player);
        checks.Same(Snapshot(4), player.DisplaySnapshot, "overflow resumes at new consumption cursor");
    }

    // 空 Tick 不占队列，未完成的非空帧仍受容量限制。
    private static void EmptyFramesDoNotConsumeQueueCapacity(CheckContext checks)
    {
        var player = Start();
        player.MaxQueuedFrames = 2;
        player.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }));
        for (var tick = 2; tick <= 4; tick++) player.Submit(Frame(Snapshot(tick), 1));
        checks.Check(!player.IsIdle && player.Diagnostic == null && player.ActiveCues.Count == 1,
            "empty Ticks update immediately and occupy no queued frame storage");
        checks.Same(Snapshot(4), player.DisplaySnapshot, "latest empty frame is visible during old damage fade");
        player.Submit(Frame(Snapshot(5), 1, new[] { Damage(2) with { Tick = 5 } }));
        player.Submit(Frame(Snapshot(6), 2, new[] { Damage(3) with { Tick = 6 } }));
        checks.Check(player.IsIdle && player.Diagnostic?.Contains("超限") == true,
            "unfinished nonempty frames remain bounded under cross Tick parallelism");
    }
}
