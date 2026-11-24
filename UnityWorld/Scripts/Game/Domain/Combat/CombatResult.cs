namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>记录原卡本次开战、耗用后和正式战斗结算后的次数。</summary>
    /// <param name="CardId">所属世界卡实例 ID。</param>
    /// <param name="InitialCount">开战时的次数。</param>
    /// <param name="RemainingCount">本场实际耗用后剩余的次数。</param>
    /// <param name="FinalCount">计入准备后的世界回写次数。</param>
    public sealed record CombatConsumptionResult(int CardId, int InitialCount, int RemainingCount, int FinalCount);

    /// <summary>
    /// 单个参战者的战斗结算数据（用于回写大世界）
    /// </summary>
    public class CombatantResult
    {
        /// <summary>对应的大世界 NpcId</summary>
        public int NpcId { get; set; }

        /// <summary>所属阵营</summary>
        public CombatTeam Team { get; set; }

        /// <summary>战斗结束时的状态</summary>
        public CombatantStatus FinalStatus { get; set; }

        /// <summary>预留的本场 HP 总损耗，当前结算未填入</summary>
        public float HpLost { get; set; }

        /// <summary>是否在本场战斗中被击败</summary>
        public bool IsDefeated => FinalStatus == CombatantStatus.Defeated;

        /// <summary>本场战斗内共行动的回合数</summary>
        public int TurnsActed { get; set; }

        /// <summary>参战消耗卡的次数结算，含已离场卡。</summary>
        public IReadOnlyList<CombatConsumptionResult> Consumption { get; init; } = Array.Empty<CombatConsumptionResult>();
    }

    /// <summary>
    /// 战斗系统统一结果工厂。
    /// 继承 ContextBase，所有战斗结算数据通过 Set/Get 键值对传递。
    /// </summary>
    public class CombatResult : ContextBase
    {
        /// <summary>保护消耗次数回写的锁</summary>
        private readonly object _commitLock = new();
        /// <summary>消耗次数是否已回写</summary>
        private bool _consumptionCommitted;

        /// <summary>核对全部消耗卡后一次性回写次数，清除耗尽卡的备战位置</summary>
        internal void CommitConsumption(IReadOnlyList<(Npc npc, CombatTeam team)> participants)
        {
            lock (_commitLock)
            {
                if (_consumptionCommitted) return;
                var changes = new List<(Npc Npc, Card Card, CombatConsumptionResult Entry)>();
                foreach (var outcome in Get<List<CombatantResult>>("Combatants"))
                {
                    if (outcome.Consumption.Count == 0) continue;
                    var npc = participants.Single(item => item.npc.Id == outcome.NpcId).npc;
                    var held = npc.GetAllCards().ToDictionary(card => card.Id);
                    foreach (var entry in outcome.Consumption)
                    {
                        if (!held.TryGetValue(entry.CardId, out var card) || !card.HasKeyword("Consume") ||
                            card.GetStack() != entry.InitialCount || entry.FinalCount < 0 || entry.FinalCount > card.GetStackMax())
                            throw new InvalidOperationException($"卡 {entry.CardId} 的世界消耗数据在战斗期间发生变化。");
                        changes.Add((npc, card, entry));
                    }
                }

                // 先核对全部实例，再提交次数；零次只清备战位置和选中标记，保留持有。
                foreach (var (npc, card, entry) in changes)
                {
                    if (entry.FinalCount == 0) npc.UnassignCard(card.Id);
                    card.BaseData.Stack = entry.FinalCount;
                }
                _consumptionCommitted = true;
            }
        }
        // ── 战斗场景结算 ────────────────────────────────────────

        /// <summary>整场战斗结算结果（CombatScene.End() 生成，抛回大世界）</summary>
        public static CombatResult CombatSceneResult(
            CombatEndReason endReason,
            CombatTeam winnerTeam,
            int totalTicks,
            List<CombatantResult> combatants)
        {
            var ret = new CombatResult();
            ret.Set("EndReason", endReason);
            ret.Set("WinnerTeam", winnerTeam);
            ret.Set("TotalTicks", totalTicks);
            ret.Set("Combatants", combatants);
            return ret;
        }

        // ── 入槽结果 ───────────────────────────────────────────

        /// <summary>待发槽入槽结果</summary>
        public static CombatResult SlotPushResult(
            bool overflowed,
            ContestData overflowedContestData,
            CombatNpc overflowTarget,
            bool triggeredContest,
            (CombatNpc attacker, CombatNpc target)? contestPair)
        {
            var ret = new CombatResult();
            ret.Set("Overflowed", overflowed);
            ret.Set("OverflowedContestData", overflowedContestData);
            ret.Set("OverflowTarget", overflowTarget);
            ret.Set("TriggeredContest", triggeredContest);
            if (contestPair != null)
                ret.Set("ContestPair", contestPair);
            return ret;
        }

        // ── 对拼结算 ───────────────────────────────────────────

        /// <summary>对拼结算结果</summary>
        public static CombatResult ContestResult(
            CombatNpc winner,
            CombatNpc loser,
            float damageValue,
            float healValue,
            bool hpZeroed,
            DamageInfo context,
            bool isDraw,
            bool isEmpty)
        {
            var ret = new CombatResult();
            ret.Set("Winner", winner);
            ret.Set("Loser", loser);
            ret.Set("DamageValue", damageValue);
            ret.Set("HealValue", healValue);
            ret.Set("HpZeroed", hpZeroed);
            ret.Set("Context", context);
            ret.Set("IsDraw", isDraw);
            ret.Set("IsEmpty", isEmpty);
            return ret;
        }

        // ── 直击结算 ───────────────────────────────────────────

        /// <summary>直击结算结果</summary>
        public static CombatResult DirectHitResult(
            CombatNpc target,
            float damageValue,
            bool hpZeroed,
            DamageInfo context)
        {
            var ret = new CombatResult();
            ret.Set("Target", target);
            ret.Set("DamageValue", damageValue);
            ret.Set("HpZeroed", hpZeroed);
            ret.Set("Context", context);
            return ret;
        }
    }

}
