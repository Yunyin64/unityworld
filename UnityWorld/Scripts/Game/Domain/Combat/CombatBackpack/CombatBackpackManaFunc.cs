#nullable enable annotations
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public sealed partial class CombatBackpack
    {
        // 背包只保存格上灵元并协调转移；接收资格与已蓄灵状态由卡自身负责。
        /// <summary>可替换的选卡距离函数；默认取源格到卡牌最近占格的曼哈顿距离。</summary>
        public Func<BackpackCell, IReadOnlyList<BackpackCell>, int> ManaTargetDistance { get; set; }
            = (source, cells) => cells.Min(c => Math.Abs(c.X - source.X) + Math.Abs(c.Y - source.Y));

        /// <summary>
        /// 只选择可接收灵元的卡，不转移资源。距离函数保留为策略入口，当前默认算法非最终玩法定案。
        /// 同距先下方、再左方，最后按实例 Id 排序，保持现有重放顺序。
        /// </summary>
        public CombatCard? SelectManaTarget(BackpackCell source, ElementType element, IEnumerable<CombatCard> cards)
        {
            return cards.Where(c => ReferenceEquals(c.Owner?.Backpack, this) && Placements.ContainsKey(c.Id) && c.CanAcceptMana(element))
                .OrderBy(c => ManaTargetDistance(source, Placements[c.Id]))
                .ThenByDescending(c => Placements[c.Id].Max(p => p.Y))
                .ThenBy(c => Placements[c.Id].Min(p => p.X))
                .ThenBy(c => c.Id).FirstOrDefault();
        }

        /// <summary>选择目标并转移源格灵元；没有可接收卡时留在原格，再次处理已释放的格不会重复分配。</summary>
        public CombatCard? AssignManaAt(BackpackCell source, IEnumerable<CombatCard> cards)
        {
            if (!Mana.TryGetValue(source, out var element)) return null;
            var selected = SelectManaTarget(source, element, cards);
            return selected != null && TryAssign(source, selected) ? selected : null;
        }

        /// <summary>生成/测试布置统一入口，不允许覆盖卡或已存在的灵元。</summary>
        public void PlaceMana(BackpackCell cell, ElementType element)
        {
            if (!_validCells.Contains(cell) || _cardCells.ContainsKey(cell) || _mana.ContainsKey(cell))
                throw new InvalidOperationException($"灵元无法占据格 ({cell.X + 1},{cell.Y + 1})。");
            if (element.Equals(ElementType.None)) throw new InvalidOperationException("灵元必须有元素。");
            _mana.Add(cell, element);
            PresentationOwner?.Scene?.MarkPresentationEffect();
        }

        /// <summary>从空间转移到卡上，原格立即释放；资源不会复制。</summary>
        public bool TryAssign(BackpackCell cell, CombatCard card)
        {
            // 同源战斗可能复用卡 Id，必须确认接收卡属于当前背包实例。
            if (!ReferenceEquals(card.Owner?.Backpack, this) || !_placements.ContainsKey(card.Id) ||
                !_mana.TryGetValue(cell, out var element) || !card.CanAcceptMana(element)) return false;
            var scene = card.Scene;
            var before = scene?.PresentationRecordingEnabled == true
                ? CombatScene.PresentationCopy(card.AssignedElements.Select(e => e.ToString())) : null;
            // 接收不触发回调；成功后立即释放源格，再记录独立复制的表现事实。
            if (!card.TryReceiveMana(element)) return false;
            _mana.Remove(cell);
            if (before != null)
                scene.RecordPresentation(CombatPresentationKind.ManaAssigned, card.Owner, card,
                    mana: new ManaAssignedPresentation(new PresentationCell(cell.X, cell.Y), element.ToString(), before,
                        CombatScene.PresentationCopy(card.AssignedElements.Select(e => e.ToString()))));
            return true;
        }

        /// <summary>从格上实体稳定排序后不放回抽取并移除，数量不足部分成功，不触碰卡上点亮。</summary>
        public IReadOnlyList<KeyValuePair<BackpackCell, ElementType>> RemoveMana(int amount, ElementType? element, SoulData soul)
        {
            if (amount <= 0) return Array.Empty<KeyValuePair<BackpackCell, ElementType>>();
            var candidates = _mana.Where(p => !element.HasValue || p.Value.Equals(element.Value))
                .OrderBy(p => p.Key.Y).ThenBy(p => p.Key.X).ToList();
            var count = Math.Min(amount, candidates.Count);
            var removed = new List<KeyValuePair<BackpackCell, ElementType>>(count);
            for (int i = 0; i < count; i++)
            {
                var index = soul.Random(0, candidates.Count);
                var selected = candidates[index];
                candidates.RemoveAt(index);
                _mana.Remove(selected.Key);
                removed.Add(selected);
            }
            if (removed.Count > 0) PresentationOwner?.Scene?.MarkPresentationEffect();
            return removed.AsReadOnly();
        }
    }
}
