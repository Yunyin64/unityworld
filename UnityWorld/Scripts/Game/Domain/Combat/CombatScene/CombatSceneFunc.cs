namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatScene
    {
        // 同一批里每个到行的角色先各扫一行。充能带来的多余进度留给下一批。
        private void ScanReadyRows()
        {
            var batches = 0;
            while (!IsFinished)
            {
                var due = ActiveCombatants.Where(npc => npc.Backpack.ScanProgress >= TicksPerRow).ToList();
                if (due.Count == 0) return;
                if (++batches > 10000)
                {
                    SetPhaseFinished();
                    throw new InvalidOperationException($"扫描进度循环：Tick={CurrentTick} batches={batches}; " +
                        string.Join("; ", due.Select(npc => $"Npc={npc.Id} progress={npc.Backpack.ScanProgress}")) +
                        "；最近事件/扫描：" + string.Join(" | ", TraceLog.TakeLast(20)));
                }

                foreach (var npc in due)
                {
                    npc.Backpack.TryTakeRow(TicksPerRow);
                    //Trace(npc, $"扫描行 余量={npc.Backpack.ScanProgress}");
                    npc.ScanRow();
                }
                ResolveBoundaryContests(due);
                FinishScanRows(due);
                AssignTargets();

                // 一方败退即停止后续扫描。若双方仍在场，当前 Tick 的全部扫描批次都应处理。
                if (HasAtMostOneActiveTeam()) CheckEndConditions();
            }
        }

        private static void ResolveBoundaryContests(List<CombatNpc> due)
        {
            // 先固定本批的双方待发槽，再统一执行。前一对拼点的回调可能改变后续待发槽。
            var paired = new HashSet<int>();
            var pairs = new List<(CombatNpc Owner, ContestData Own, CombatNpc Target, ContestData Other)>();
            foreach (var npc in due)
            {
                var target = npc.GetTarget();
                if (target == null || npc.PendingSlot == null || target.PendingSlot == null ||
                    paired.Contains(npc.Id) || paired.Contains(target.Id)) continue;

                paired.Add(npc.Id);
                paired.Add(target.Id);
                pairs.Add((npc, npc.PendingSlot, target, target.PendingSlot));
            }
            foreach (var pair in pairs)
                pair.Owner.ProcessContestAtBoundary(pair.Own, pair.Target, pair.Other);
        }

        private void FinishScanRows(List<CombatNpc> due)
        {
            var endingRounds = due.Where(npc => npc.Backpack.Row == npc.Backpack.Height - 1).ToArray();
            foreach (var npc in endingRounds) npc.ForceOwnPending();
            // 即使没人到轮末，本行产生的伤害和离场也要在换行前结算。
            FlushCombatChanges();

            foreach (var npc in endingRounds)
                if (npc.IsActive) npc.DispatchRoundEvent("OnRoundEnd");
            if (endingRounds.Length > 0) FlushCombatChanges();
            foreach (var npc in endingRounds) npc.ExpireRoundEffects();

            // 轮首监听可跨阵营施加效果。先让同批角色全部换行，再执行新轮回调。
            var startingRounds = new List<CombatNpc>();
            foreach (var npc in due)
            {
                if (!npc.Backpack.FinishRow()) continue;
                Trace(npc, "本方新轮");
                if (npc.IsActive) startingRounds.Add(npc);
            }
            foreach (var npc in startingRounds)
                if (npc.IsActive) npc.BeginBackpackRound();
            FlushCombatChanges();
        }

        private void FlushCombatChanges()
        {
            foreach (var npc in OrderedCombatants) npc.DealDamage();
            foreach (var npc in OrderedCombatants) npc.DealFieldChange();
            foreach (var npc in OrderedCombatants) npc.CheckDefeated();
        }

        private void AssignTargets()
        {
            foreach (var npc in ActiveCombatants)
            {
                if (npc.GetTarget()?.IsActive == true) continue;
                npc.ChangeTarget(ActiveCombatants.FirstOrDefault(other => other.Team != npc.Team));
            }
        }
    }
}
