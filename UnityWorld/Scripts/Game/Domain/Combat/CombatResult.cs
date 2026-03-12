namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 单个参战者的战斗结算数据（用于回写大世界）
    /// </summary>
    public class CombatantResult
    {
        /// <summary>对应的大世界 NpcId</summary>
        public NpcId NpcId { get; set; }

        /// <summary>所属阵营</summary>
        public CombatTeam Team { get; set; }

        /// <summary>战斗结束时的状态</summary>
        public CombatantStatus FinalStatus { get; set; }

        /// <summary>本场战斗承受的 HP 总损耗（正数 = 损失）</summary>
        public float HpLost { get; set; }

        /// <summary>是否在本场战斗中被击败</summary>
        public bool IsDefeated => FinalStatus == CombatantStatus.Defeated;

        /// <summary>本场战斗内共行动的回合数</summary>
        public int TurnsActed { get; set; }
    }

    /// <summary>
    /// 一场战斗的完整结算结果。
    /// CombatScene.End() 生成后抛回大世界，由调用方负责回写 Npc 状态。
    /// </summary>
    public class CombatResult
    {
        // ── 战斗概况 ──────────────────────────────────────────

        /// <summary>战斗结束原因</summary>
        public CombatEndReason EndReason { get; set; } = CombatEndReason.None;

        /// <summary>胜利阵营（平局时为 None）</summary>
        public CombatTeam WinnerTeam { get; set; } = CombatTeam.None;

        /// <summary>本场共进行的总回合数</summary>
        public int TotalTurns { get; set; }

        // ── 参战者结算 ────────────────────────────────────────

        /// <summary>所有参战者的个人结算数据</summary>
        public List<CombatantResult> Combatants { get; set; } = [];

        // ── 快捷查询 ──────────────────────────────────────────

        /// <summary>获取指定 NPC 的结算数据</summary>
        public CombatantResult? GetCombatant(NpcId id)
            => Combatants.Find(c => c.NpcId.Equals(id));

        /// <summary>获取指定阵营所有存活者（未被击败）</summary>
        public IEnumerable<CombatantResult> GetSurvivors(CombatTeam team)
            => Combatants.Where(c => c.Team == team && !c.IsDefeated);

        /// <summary>获取指定阵营所有阵亡者</summary>
        public IEnumerable<CombatantResult> GetDefeated(CombatTeam team)
            => Combatants.Where(c => c.Team == team && c.IsDefeated);

        /// <summary>从某一方视角获取胜负结果</summary>
        public CombatOutcome GetOutcome(CombatTeam team)
        {
            if (EndReason == CombatEndReason.Escaped)
                return CombatOutcome.Escaped;
            if (WinnerTeam == CombatTeam.None)
                return CombatOutcome.Draw;
            return WinnerTeam == team ? CombatOutcome.Victory : CombatOutcome.Defeat;
        }

        public override string ToString()
            => $"CombatResult(EndReason={EndReason}, Winner={WinnerTeam}, Turns={TotalTurns}, Combatants={Combatants.Count})";
    }
}
