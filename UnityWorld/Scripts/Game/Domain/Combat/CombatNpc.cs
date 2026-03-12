using UnityWorld.Game.Domain.Card;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 战斗参与者：继承自 Npc，附加战斗专用状态。
    /// 战斗内的所有变化（HP损耗、临时增益等）均记录在此实例上，
    /// 不直接影响大世界的原始 Npc 数据，结算后由 CombatResult 回写。
    ///
    /// 卡组模型说明：
    ///   每个 CombatNpc 持有一张「有序出招表」（DeckSequence），
    ///   战斗中按 CurrentDeckIndex 顺序循环出招，走完一圈算一次 CycleCount++。
    ///   无随机摸牌/弃牌机制。
    /// </summary>
    public class CombatNpc : Npc
    {
        // ── 阵营 ──────────────────────────────────────────────

        /// <summary>所属战斗阵营</summary>
        public CombatTeam Team { get; set; } = CombatTeam.None;

        // ── 战斗状态 ──────────────────────────────────────────

        /// <summary>当前战斗状态（行动中/已阵亡/逃跑/跳过）</summary>
        public CombatantStatus Status { get; set; } = CombatantStatus.Active;

        /// <summary>是否可以继续参与战斗</summary>
        public bool IsActive => Status == CombatantStatus.Active;

        // ── 战斗专用 HP 追踪 ──────────────────────────────────

        /// <summary>
        /// 战斗开始时的 HP 快照（用于结算时计算损耗）
        /// </summary>
        public float HpAtCombatStart { get; private set; }

        /// <summary>
        /// 战斗内当前 HP（与大世界 StatBlock 独立）
        /// </summary>
        public float CurrentHp { get; set; }

        // ── 卡组 ──────────────────────────────────────────────

        /// <summary>
        /// 有序出招表（固定顺序，不打乱）。
        /// 战斗期间按 CurrentDeckIndex 依次出招，走完一整圈为一个循环。
        /// </summary>
        public List<CardData> DeckSequence { get; set; } = [];

        /// <summary>
        /// 当前出招指针：指向 DeckSequence 中下一张待出的牌。
        /// 由 AdvanceDeckIndex() 推进，走完一圈自动归零。
        /// </summary>
        public int CurrentDeckIndex { get; private set; } = 0;

        /// <summary>
        /// 已完成循环次数：每走完一整张出招表加一次。
        /// </summary>
        public int CycleCount { get; private set; } = 0;

        // ── 目标 ──────────────────────────────────────────────

        /// <summary>
        /// 当前战斗目标（单向引用）。
        /// Target 被击败/逃跑后需由 CombatScene 重新分配。
        /// </summary>
        public CombatNpc? Target { get; set; } = null;

        // ── 回合计数 ──────────────────────────────────────────

        /// <summary>本场战斗内已行动的回合数</summary>
        public int TurnsActed { get; set; } = 0;

        // ── 构造 ──────────────────────────────────────────────

        public CombatNpc(NpcId id, CombatTeam team) : base(id)
        {
            Team = team;
        }

        // ── 方法 ──────────────────────────────────────────────

        /// <summary>
        /// 快照当前 HP，在 PreStart 阶段调用一次。
        /// </summary>
        public void SnapshotHp(float hp)
        {
            HpAtCombatStart = hp;
            CurrentHp = hp;
        }

        /// <summary>
        /// 重置出招指针和循环计数，在 PreStart 阶段调用。
        /// </summary>
        public void ResetDeckPointer()
        {
            CurrentDeckIndex = 0;
            CycleCount = 0;
        }

        /// <summary>
        /// 推进出招指针一位。走完整圈时 CycleCount++ 并归零。
        /// </summary>
        public void AdvanceDeckIndex()
        {
            if (DeckSequence.Count == 0) return;

            CurrentDeckIndex++;
            if (CurrentDeckIndex >= DeckSequence.Count)
            {
                CurrentDeckIndex = 0;
                CycleCount++;
            }
        }

        /// <summary>
        /// 获取当前待出的牌。出招表为空时返回 null。
        /// </summary>
        public CardData? CurrentCard
            => DeckSequence.Count > 0 ? DeckSequence[CurrentDeckIndex] : null;

        /// <summary>
        /// 应用伤害，自动检测击败条件。
        /// </summary>
        public void ApplyDamage(float amount)
        {
            CurrentHp -= amount;
            if (CurrentHp <= 0f)
            {
                CurrentHp = 0f;
                Status = CombatantStatus.Defeated;
            }
        }

        /// <summary>
        /// 应用治疗，不超过初始HP上限。
        /// </summary>
        public void ApplyHeal(float amount)
        {
            CurrentHp = Math.Min(CurrentHp + amount, HpAtCombatStart);
        }

        public override string ToString()
            => $"CombatNpc({Id.Value}, Team={Team}, HP={CurrentHp}/{HpAtCombatStart}, Status={Status}, DeckIdx={CurrentDeckIndex}/{DeckSequence.Count}, Cycle={CycleCount})";
    }
}