namespace Xianxia.Client.Battle;

/// <summary>值夹具驱动真实调度器和归并器；不初始化世界，也不模拟核心规则。</summary>
public static partial class PresentationPlayerChecks
{
    /// <summary>按原有顺序执行全部场景，并输出本次运行的统计。</summary>
    public static void Run()
    {
        // 统计只属于本次运行，避免重复运行或并行调用互相污染。
        var checks = new CheckContext();
        checks.Test(InitialAndEmptyFramesDisplayImmediately);
        checks.Test(SameCardAbsorptionPrecedesUse);
        checks.Test(UnrelatedCardsAnimateInParallel);
        checks.Test(HistoricalCollisionPreservesReplacement);
        checks.Test(DrawAndDirectHitUseActualParticipants);
        checks.Test(DuplicateFramesSettleOnlyOnce);
        checks.Test(InvalidFramesRecoverFromCompleteSnapshot);
        checks.Test(InvalidEventSequencesAreRejected);
        checks.Test(OversizedBatchSkipsAnimations);
        checks.Test(EventOverflowCancelsOldGeneration);
        checks.Test(EmptyFramesDoNotConsumeQueueCapacity);
        checks.Test(AnimationTogglePreservesConsumptionCursor);
        checks.Test(TerminalSnapshotWaitsForFinalAnimation);
        checks.Test(ResetRejectsRetiredSessionFrames);
        checks.Test(ResetInsideCallbacksIsSafe);
        checks.Test(LargeDeltaMatchesSmallStepsWithoutMutatingInput);
        checks.Test(CardInsertionPrecedesDependentEvents);
        checks.Test(SameTickRemovalWinsAfterInsertion);
        checks.Test(OrdinaryStateUpdatesWhileDamageWaits);
        checks.Test(PassiveEffectWaitsForItsCardInsertion);
        checks.Test(CrossTickAnimationsOverlapWithoutBlockingProgress);
        checks.Test(CrossTickCardChainPreservesLatestTick);
        checks.Test(ConcurrentHeadTextPreservesLatestHp);
        checks.Test(OldHeadTextCannotOverwriteNewSnapshot);
        checks.Test(OldCollisionReleasesSlotToReplacement);
        checks.Test(OldInsertionCannotResurrectRemovedCard);
        checks.Test(OldManaFlightPreservesReusedCell);
        checks.Test(InvalidDisplayDeltaIsRejected);

        Console.WriteLine($"PRESENTATION_PLAYER_OK scenarios={checks.Scenarios} assertions={checks.Assertions} causalOrder=true parallel=true bounded=true resetSafe=true inputImmutable=true");
    }

    // 共享断言与收尾规则；每个场景的播放器和夹具仍由场景自己创建。
    private sealed class CheckContext
    {
        public int Assertions { get; private set; }
        public int Scenarios { get; private set; }

        public void Test(Action<CheckContext> scenario)
        {
            Scenarios++;
            try
            {
                scenario(this);
            }
            catch (Exception exception)
            {
                // 具名方法使断言失败和意外异常都能直接定位到具体场景。
                throw new InvalidOperationException(
                    $"PRESENTATION_PLAYER_FAILED {scenario.Method.Name}: {exception.Message}", exception);
            }
        }

        public void Check(bool condition, string message)
        {
            Assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }

        public void Same(BattleSnapshot expected, BattleSnapshot actual, string message) =>
            Check(BattleJson.SerializeSnapshot(expected) == BattleJson.SerializeSnapshot(actual), message);

        public void Drain(BattlePresentationPlayer player)
        {
            player.Update(1000);
            Check(player.IsIdle && player.ActiveCues.Count == 0, "all committed animations settle");
        }
    }
}
