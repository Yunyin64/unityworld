using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public sealed partial class CombatBackpack
    {
        // 卡牌完整形状的选位、插入和移除。
        /// <summary>寻找保持完整形状的位置。优先纯空位，再按行序尝试灵元让位。</summary>
        public IReadOnlyList<BackpackCell> FindPlacement(Card card, bool replaceMana)
        {
            NpcBackpackLayout.ValidateCard(card);
            foreach (var allowMana in replaceMana ? new[] { false, true } : new[] { false })
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var cells = card.BaseData.Shape.Select(c => new BackpackCell(x + c.X, y + c.Y)).ToArray();
                if (cells.All(c => _validCells.Contains(c) && !_cardCells.ContainsKey(c) && (allowMana || !_mana.ContainsKey(c))))
                    return Array.AsReadOnly(cells);
            }
            return null;
        }

        /// <summary>原子地插入整张卡，返回实际被替代的灵元位置；不拆格或移动其他卡。</summary>
        public IReadOnlyList<BackpackCell> Insert(Card card, bool replaceMana)
        {
            if (_placements.ContainsKey(card.Id)) throw new InvalidOperationException("卡牌已在背包中。");
            var cells = FindPlacement(card, replaceMana) ?? throw new InvalidOperationException($"无法容纳完整卡牌形状：{card.DefineId}。布局违反完整放置的设计约束，请修正数据。");
            var replaced = cells.Where(_mana.ContainsKey).ToArray();
            foreach (var cell in cells) { _mana.Remove(cell); _cardCells.Add(cell, card.Id); }
            _placements.Add(card.Id, Array.AsReadOnly(cells.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray()));
            card.BaseData.Position = BackpackCoordinates.ToPosition(new BackpackCell(cells.Min(c => c.X), cells.Min(c => c.Y)));
            return Array.AsReadOnly(replaced);
        }

        internal void RemoveCard(int cardId)
        {
            if (cardId == MainCardId) throw new InvalidOperationException("主功法固定卡不能移除。");
            if (!_placements.Remove(cardId, out var cells)) return;
            foreach (var cell in cells) _cardCells.Remove(cell);
        }
    }
}
