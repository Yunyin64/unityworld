using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard
    {
        // ── 装备与卡组 ──

        /// <summary>读取同场战斗的装备快照及有效战斗修正。</summary>
        public override Equip GetEquip() => Owner?.ReadBattleEquipment(Id);

        /// <summary>按编号查战斗内可用的装备父卡</summary>
        protected override Card FindEquipmentCard(int id) => Owner?.FindBattleCard(id);

        /// <summary>读取带战斗临时修正的绑定装备</summary>
        protected override Equip ReadBoundEquipment(Card equipmentCard) => Owner?.ReadBattleEquipment(equipmentCard.Id);

        /// <summary>本卡在持有者场上卡牌列表中的位置。</summary>
        public int GetDeckIndex() => Owner?.GetField().IndexOf(this) ?? -1;

        // ── 阶段与计数 ──

        /// <summary>旧版就绪状态字段</summary>
        private bool isReady;
        /// <summary>写入旧脚本可读的就绪标记。</summary>
        public void SetReady(bool ready) => isReady = ready;
        /// <summary>读取旧脚本的就绪标记。</summary>
        public bool IsReady() => isReady;

        // 法术与招式每次发动只结算一次。法宝发动与常驻被动的开场结算各走自己的时序。
        /// <summary>已提交的用卡是否仍待生效</summary>
        private bool AwaitingApply => UseCount != ApplyCount && CallMainKeyword("PaidUseAwaitsApply") is true;

        // 只在发动提交时累计；离场再入场沿用本轮次数，由所属背包换轮时清零。
        /// <summary>所属背包本轮已经使用的招式次数。</summary>
        public int FormUsesThisRound { get; internal set; }

        /// <summary>主关键词在招式成功提交时记录一次使用。</summary>
        public void RecordFormUse() => FormUsesThisRound++;

        // ── 固定与冻结 ──

        /// <summary>锚定持续至哪一轮</summary>
        private int _rootedThroughRound;

        /// <summary>当前轮是否仍受锚定</summary>
        internal bool IsRooted => Owner != null && _rootedThroughRound >= Owner.Backpack.Round;

        /// <summary>延长在场卡牌的锚定期限</summary>
        internal void Root(int rounds)
        {
            if (rounds <= 0 || Owner == null || !Owner.Backpack.Placements.ContainsKey(Id)) return;
            int lastRound = Owner.GetLastEffectRound(rounds);
            if (lastRound <= _rootedThroughRound) return;
            _rootedThroughRound = lastRound;
            Scene?.MarkPresentationEffect();
        }

        /// <summary>本卡是否会在下次完整扫描时跳过发动并解冻。</summary>
        public bool IsFrozen { get; private set; }
        /// <summary>按方向和关键词生效的抗冻结光环</summary>
        private readonly HashSet<(string Direction, string Keyword)> _freezeImmunityAuras = new();

        /// <summary>注册覆盖指定方位和关键词的抗冻结光环</summary>
        internal void GrantFreezeImmunityAura(string direction, string keyword)
        {
            if (direction is not ("All" or "Up" or "Down" or "Left" or "Right" or "Neighbor")) return;
            if (_freezeImmunityAuras.Add((direction, keyword))) Scene?.MarkPresentationEffect();
        }

        // 冻结时按当前布局查覆盖，来源失活和滑移会即时反映在判定中。
        /// <summary>检查本卡是否处在可响应的抗冻结光环内</summary>
        internal bool IsFreezeImmune()
        {
            if (Owner == null || !Owner.Backpack.Placements.TryGetValue(Id, out var cells)) return false;
            foreach (var source in Owner.GetField())
            {
                if (source._freezeImmunityAuras.Count == 0 || source.CallMainKeyword("CanRespond", argument: true) is not true) continue;
                var sourceCells = Owner.Backpack.GetCardCells(source.Id);
                foreach (var (direction, keyword) in source._freezeImmunityAuras)
                {
                    if (keyword != "Any" && !HasKeyword(keyword)) continue;
                    if (direction == "All" || (source != this && CombatBackpack.HasSharedEdge(sourceCells, cells, direction)))
                        return true;
                }
            }
            return false;
        }

        /// <summary>尝试冻结卡牌一次，先检查当前光环覆盖。</summary>
        public bool Freeze()
        {
            if (IsFrozen || IsFreezeImmune()) return false;
            IsFrozen = true;
            Scene?.MarkPresentationEffect();
            Log("冻结：下次完整扫描跳过后解除");
            return true;
        }
        /// <summary>立即解除已经生效的冻结，主动发动由末格扫描处理。</summary>
        internal bool Thaw()
        {
            if (!IsFrozen) return false;
            IsFrozen = false;
            Scene?.MarkPresentationEffect();
            return true;
        }

        /// <summary>在完整扫描末格解冻，并报告是否因此跳过发动</summary>
        internal bool ConsumeFreezeAtScanEnd()
        {
            if (!Thaw()) return false;
            Log("完整扫描：跳过主动机会并解冻");
            return true;
        }

    }
}
