using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public sealed partial class CombatBackpack
    {
        private static readonly CombatCardSlideType[] SlideDirections =
            [CombatCardSlideType.Up, CombatCardSlideType.Down, CombatCardSlideType.Left, CombatCardSlideType.Right];

        // Lua 可以直接传枚举；已有模板和空间条件传方向名。数字和未知名称都没有方向。
        internal static CombatCardSlideType ReadSlideDirection(object value) => value switch
        {
            CombatCardSlideType direction => direction,
            string name when Enum.IsDefined(typeof(CombatCardSlideType), name) => Enum.Parse<CombatCardSlideType>(name),
            _ => CombatCardSlideType.None
        };

        private static BackpackCell DirectionOffset(CombatCardSlideType direction) => direction switch
        {
            CombatCardSlideType.Up => new(0, -1),
            CombatCardSlideType.Down => new(0, 1),
            CombatCardSlideType.Left => new(-1, 0),
            CombatCardSlideType.Right => new(1, 0),
            _ => default
        };

        internal static bool HasSharedEdge(IReadOnlyList<BackpackCell> reference,
            IReadOnlyList<BackpackCell> candidate, string direction)
        {
            if (direction == "Neighbor")
                return reference.Any(a => candidate.Any(b => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1));

            var offset = DirectionOffset(ReadSlideDirection(direction));
            return offset != default && reference.Any(cell => candidate.Contains(new(cell.X + offset.X, cell.Y + offset.Y)));
        }

        internal bool TrySlide(CombatCard card, CombatCardSlideType direction, CombatNpc mover, CombatCard sourceCard)
        {
            if (!ReferenceEquals(card?.Owner?.Backpack, this)) return false;
            var id = card.Id;
            if (id == MainCardId || card.IsRooted ||
                card.CheckPhase(CombatCardPhase.InPending) || ReferenceEquals(card.Owner.PendingSlot?.SourceCard, card) ||
                !_placements.TryGetValue(id, out var oldCells)) return false;

            if (direction == CombatCardSlideType.Random)
            {
                // 每个能迈出第一步的方向只算一次，能滑几格不影响抽中概率。
                var available = SlideDirections.Where(candidate => CanSlideTo(id, oldCells, DirectionOffset(candidate), 1)).ToArray();
                if (available.Length == 0) return false;
                direction = available.Length == 1 ? available[0] : available[card.Scene.Soul.Random(0, available.Length)];
            }

            var offset = DirectionOffset(direction);
            if (offset == default) return false;
            int distance = 0;
            while (CanSlideTo(id, oldCells, offset, distance + 1)) distance++;
            if (distance == 0) return false;

            // 确定终点后才改布局，沿途灵元保留。事件中的 direction 此时已是实际方向。
            var destination = oldCells.Select(cell => new BackpackCell(cell.X + offset.X * distance, cell.Y + offset.Y * distance)).ToArray();
            var placement = _layout.PlacedCards.Single(entry => entry.CardId == id);
            var from = placement.Position ?? throw new InvalidOperationException($"场上卡 {id} 缺少锚点。");
            var to = new BackpackPosition(from.X + offset.X * distance, from.Y + offset.Y * distance);
            foreach (var cell in oldCells) _cardCells.Remove(cell);
            foreach (var cell in destination)
            {
                _mana.Remove(cell);
                _cardCells.Add(cell, id);
            }

            _placements[id] = Array.AsReadOnly(destination);
            placement.Position = to;
            if (destination.Any(cell => cell.Y < Row || (cell.Y == Row && cell.X <= ScannedColumn)))
                _movedPastScan[id] = Round;
            else
                _movedPastScan.Remove(id);
            PresentationOwner?.Scene?.MarkPresentationEffect();

            // 占格和扫描状态已提交。从被位移一侧报告结果。
            var moved = new APIContext { Caster = card.Owner, SourceCard = card, Scene = card.Scene };
            // 监听可能晚于下一次位移。快照保留提交时的参照卡和目标卡完整占格。
            moved.EventPlacements = _placements.ToDictionary(pair => pair.Key, pair => pair.Value);
            moved.Set("MovedCard", card);
            // 位移事件对 Lua 沿用方向名，脚本仍可直接和 "Right" 等字符串比较。
            moved.Set("Direction", direction.ToString());
            moved.Set("OldX", from.X);
            moved.Set("OldY", from.Y);
            moved.Set("NewX", to.X);
            moved.Set("NewY", to.Y);
            var scene = moved.Scene;
            scene.TriggerCombatEvent("OnCardDisplaced", moved);
            if (mover != null && !ReferenceEquals(mover, moved.Caster))
            {
                // 同一次位移的坐标和目标相同，发起方事件使用发起者的身份。
                var displacedOther = moved.CopyForEvent();
                displacedOther.Caster = mover;
                displacedOther.SourceCard = sourceCard;
                scene.TriggerCombatEvent("OnDisplaceOtherCard", displacedOther);
            }
            return true;
        }

        private bool CanSlideTo(int cardId, IReadOnlyList<BackpackCell> cells, BackpackCell offset, int distance)
        {
            foreach (var cell in cells)
            {
                var next = new BackpackCell(cell.X + offset.X * distance, cell.Y + offset.Y * distance);
                if (!_validCells.Contains(next) || (_cardCells.TryGetValue(next, out var occupant) && occupant != cardId))
                    return false;
            }
            return true;
        }

        // 卡牌完整形状的选位、插入和移除。
        /// <summary>寻找保持完整形状的位置。优先纯空位，再按行序尝试灵元让位。</summary>
        public IReadOnlyList<BackpackCell> FindPlacement(Card card, bool replaceMana)
        {
            NpcSystemCard.ValidateCard(card);
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
            // 锚点只写本场布局；运行时新增卡及 Field 标记也只进入本场卡数据。
            _layout.PlacedCards.Add(new()
            {
                CardId = card.Id,
                Position = BackpackCoordinates.ToPosition(new BackpackCell(cells.Min(c => c.X), cells.Min(c => c.Y)))
            });
            _cardData.Reserve.Remove(card.Id);
            if (!_cardData.Field.Contains(card.Id)) _cardData.Field.Add(card.Id);
            if (!_cardData.AllCardIds.Contains(card.Id))
            {
                _cardData.AllCardIds.Add(card.Id);
                _cardData.AllCards.Add(card);
            }
            return Array.AsReadOnly(replaced);
        }

        internal void RemoveCard(int cardId)
        {
            if (cardId == MainCardId) throw new InvalidOperationException("主功法固定卡不能移除。");
            if (!_placements.Remove(cardId, out var cells)) return;
            _movedPastScan.Remove(cardId);
            foreach (var cell in cells) _cardCells.Remove(cell);
            _layout.PlacedCards.RemoveAll(entry => entry.CardId == cardId);
            _cardData.Field.Remove(cardId);
        }
    }
}
