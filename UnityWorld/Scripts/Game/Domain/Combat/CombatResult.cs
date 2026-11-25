using UnityWorld.Game.Data;

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

        /// <summary>本场新获的普通伤口定义，每张保留一项。</summary>
        internal IReadOnlyList<string> GainedWounds { get; set; } = Array.Empty<string>();

        /// <summary>参战消耗卡的次数结算，含已离场卡。</summary>
        public IReadOnlyList<CombatConsumptionResult> Consumption { get; init; } = Array.Empty<CombatConsumptionResult>();
    }

    /// <summary>
    /// 战斗系统统一结果工厂。
    /// 继承 ContextBase，所有战斗结算数据通过 Set/Get 键值对传递。
    /// </summary>
    public class CombatResult : ContextBase
    {
        /// <summary>保护本场全部世界回写的锁</summary>
        private readonly object _commitLock = new();
        /// <summary>本场正式结果是否已提交</summary>
        private bool _committed;

        /// <summary>先核对所有参战者，再提交消耗次数和普通伤口。</summary>
        internal void CommitToWorld(IReadOnlyList<(Npc npc, CombatTeam team)> participants)
        {
            lock (_commitLock)
            {
                if (_committed) return;
                var changes = new List<(Npc Npc, Card Card, CombatConsumptionResult Entry)>();
                var gained = new List<(Npc Npc, CardDefine Define)>();
                foreach (var outcome in Get<List<CombatantResult>>("Combatants"))
                {
                    var npc = participants.Single(item => item.npc.Id == outcome.NpcId).npc;
                    var held = NpcMgr.Instance.CardSystem.GetWorldData(npc).AllCards.ToDictionary(card => card.Id);
                    foreach (var defineId in outcome.GainedWounds)
                    {
                        var define = CardDefineMgr.Instance.Get(defineId)
                            ?? throw new InvalidOperationException($"缺少待提交的伤口定义 {defineId}。");
                        gained.Add((npc, define));
                    }
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
                foreach (var (npc, define) in gained) npc.GainCard(CardMgr.Instance.InstantiateFromDefine(define));
                _committed = true;
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
