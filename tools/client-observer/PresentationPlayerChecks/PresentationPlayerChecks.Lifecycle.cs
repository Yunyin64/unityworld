using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

public static partial class PresentationPlayerChecks
{
    // 关闭动画立即提交快照，再开启时不会重播已消费事件。
    private static void AnimationTogglePreservesConsumptionCursor(CheckContext checks)
    {
        var player = Start();
        player.AnimationsEnabled = false;
        player.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }));
        checks.Check(player.IsIdle && player.StartedCueCount == 0, "disabled animation uses snapshot immediately");
        checks.Same(Snapshot(1), player.DisplaySnapshot, "disabled animation final correctness");
        player.AnimationsEnabled = true;
        player.Submit(Frame(Snapshot(2), 1, new[] { Damage(2) with { Tick = 2 } }));
        checks.Drain(player);
        checks.Check(player.CompletedCueCount == 1, "re-enabled animation respects previously consumed cursor");
    }

    // 终局等待最后动画完成，暂停后的显示结果保持稳定。
    private static void TerminalSnapshotWaitsForFinalAnimation(CheckContext checks)
    {
        var player = Start();
        var terminal = Snapshot(1) with
        {
            IsFinished = true,
            Result = new("HpZero", "left", 1, Array.Empty<CombatantResultSnapshot>())
        };
        player.Submit(Frame(terminal, 0, new[] { Damage(1) }));
        checks.Check(!player.IsIdle && !player.DisplaySnapshot.IsFinished, "terminal Tick still displays its last animation");
        player.Update(.1);
        checks.Check(player.DisplaySnapshot.Tick == 1 && !player.IsIdle, "paused simulation permits display-only progress");
        checks.Drain(player);
        checks.Same(terminal, player.DisplaySnapshot, "last animation settles terminal snapshot");
        var stable = BattleJson.SerializeSnapshot(player.DisplaySnapshot);
        player.Update(600);
        checks.Check(stable == BattleJson.SerializeSnapshot(player.DisplaySnapshot), "paused terminal display remains stable");
    }

    // 重置取消旧代，迟到的旧会话帧不能复活或覆盖新会话。
    private static void ResetRejectsRetiredSessionFrames(CheckContext checks)
    {
        var player = Start(session: "old");
        var oldInitial = Frame(Snapshot(0), 0, session: "old", initialization: true);
        var oldFrame = Frame(Snapshot(1), 0, new[] { Damage(1) }, "old");
        player.Submit(oldFrame);
        var generation = player.Generation;
        player.Reset();
        checks.Check(player.Generation > generation && player.IsIdle, "reset cancels active generation");
        player.Submit(oldFrame);
        player.Submit(oldInitial);
        checks.Same(BattleSnapshot.Empty, player.DisplaySnapshot, "late old regular and initialization frames do not resurrect after reset");
        var fresh = Change(Snapshot(0), 10, n => n with { Name = "new session" });
        player.Submit(Frame(fresh, 0, session: "new", initialization: true));
        player.Submit(oldInitial);
        player.Submit(oldFrame);
        player.Update(100);
        checks.Same(fresh, player.DisplaySnapshot, "retired session cannot replace new session");
        checks.Check(player.CompletedCueCount == 0, "old reset damage never completes");
    }

    // 开始和完成回调中重置，不能继续执行旧回调或写回旧快照。
    private static void ResetInsideCallbacksIsSafe(CheckContext checks)
    {
        var first = Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" });
        var second = Mana(2, Card(201), 20, 3, Array.Empty<string>(), new[] { "Shui" });
        var player = Start();
        var starts = 0;
        player.CueStarted += _ => { starts++; player.Reset(); };
        player.Submit(Frame(Snapshot(1), 0, new[] { first, second }));
        checks.Check(starts == 1 && player.IsIdle, "reset inside start callback suppresses all remaining old callbacks");
        var completion = Start();
        completion.CueCompleted += _ => completion.Reset();
        completion.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }));
        completion.Update(100);
        checks.Check(completion.IsIdle, "reset inside completion callback does not dereference cleared frame");
        checks.Same(BattleSnapshot.Empty, completion.DisplaySnapshot, "completion callback reset is not overwritten by old snapshot");
    }

    // 负数、NaN 和无穷时间步长必须拒绝，且不改变播放状态。
    private static void InvalidDisplayDeltaIsRejected(CheckContext checks)
    {
        var player = Start();
        foreach (var invalid in new[] { -1d, double.NaN, double.PositiveInfinity })
        {
            var rejected = false;
            try { player.Update(invalid); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            checks.Check(rejected, "invalid display delta is rejected");
        }
        checks.Check(player.IsIdle, "invalid delta never changes playback state");
    }
}
