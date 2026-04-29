namespace UnityWorld.Game.Domain.Combat
{

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

        /// <summary>本场战斗承受的 HP 总损耗（正数 = 损损失）</summary>
        public float HpLost { get; set; }

        /// <summary>是否在本场战斗中被击败</summary>
        public bool IsDefeated => FinalStatus == CombatantStatus.Defeated;

        /// <summary>本场战斗内共行动的回合数</summary>
        public int TurnsActed { get; set; }
    }

    /// <summary>
    /// 战斗系统统一结果工厂。
    /// 继承 ContextBase，所有战斗结算数据通过 Set/Get 键值对传递。
    /// </summary>
    public class CombatResult : ContextBase
    {
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
