using System.Collections.ObjectModel;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>单场战斗独有的空间、灵元和扫描状态。没有世界数据引用。</summary>
    public sealed class CombatBackpack
    {
        private readonly HashSet<BackpackCell> _validCells;
        private readonly Dictionary<BackpackCell, int> _cardCells = new();
        private readonly Dictionary<int, IReadOnlyList<BackpackCell>> _placements = new();
        private readonly Dictionary<BackpackCell, ElementType> _mana = new();
        private readonly Dictionary<int, List<ElementType>> _assigned = new();
        public int Width { get; }
        public int Height { get; }
        public int Capacity => _validCells.Count;
        public int MainCardId { get; }
        public int Round { get; private set; } = 1;
        public int Row { get; private set; }
        public int TicksInRow { get; private set; }
        public IReadOnlyDictionary<BackpackCell, ElementType> Mana { get; }
        public IReadOnlyDictionary<int, IReadOnlyList<BackpackCell>> Placements { get; }

        public CombatBackpack(NpcCardData loadout)
        {
            var definition = NpcBackpackLayout.Validate(loadout);
            Width = definition.Width;
            Height = definition.Height;
            MainCardId = loadout.MainGongFaCardId!.Value;
            _validCells = definition.GetValidCells();
            foreach (var (id, cells) in loadout.Placements)
            {
                var ordered = cells.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
                _placements.Add(id, Array.AsReadOnly(ordered));
                _assigned.Add(id, []);
                foreach (var cell in ordered) _cardCells.Add(cell, id);
            }
            Mana = new ReadOnlyDictionary<BackpackCell, ElementType>(_mana);
            Placements = new ReadOnlyDictionary<int, IReadOnlyList<BackpackCell>>(_placements);
        }

        public bool IsValid(BackpackCell cell) => _validCells.Contains(cell);
        public int? CardAt(BackpackCell cell) => _cardCells.TryGetValue(cell, out var id) ? id : null;
        public bool IsCardCompleteAt(int cardId, BackpackCell cell) => _placements[cardId][^1] == cell;
        public IReadOnlyList<ElementType> AssignedTo(int cardId) => _assigned[cardId].AsReadOnly();
        public int LitCount(int cardId) => _assigned[cardId].Count;
        /// <summary>一次使用已成立，结束本次点亮；不是另一次灵元支付。</summary>
        internal void BeginNextActivation(int cardId) => _assigned[cardId].Clear();
        public List<BackpackCell> EmptyCells() => _validCells.Where(c => !_cardCells.ContainsKey(c) && !_mana.ContainsKey(c))
            .OrderBy(c => c.Y).ThenBy(c => c.X).ToList();

        /// <summary>生成/测试布置统一入口，不允许覆盖卡或已存在的灵元。</summary>
        public void PlaceMana(BackpackCell cell, ElementType element)
        {
            if (!_validCells.Contains(cell) || _cardCells.ContainsKey(cell) || _mana.ContainsKey(cell))
                throw new InvalidOperationException($"灵元无法占据格 ({cell.X},{cell.Y})。");
            if (element.Equals(ElementType.None)) throw new InvalidOperationException("灵元必须有元素。");
            _mana.Add(cell, element);
        }

        /// <summary>只接收仍缺少的匹配元素；数量和匹配要求来自卡的资源定义。</summary>
        public bool CanAccept(CombatCard card, ElementType element)
        {
            return card.BaseData.ScanActivatable && !card.CheckPhase(CombatCardPhase.InPending) && _assigned.TryGetValue(card.Id, out var resources) &&
                resources.Count < card.GetSize() && card.GetManaCost().TryGetValue(element, out var required) &&
                resources.Count(e => e.Equals(element)) < required;
        }

        /// <summary>从空间转移到卡上，原格立即释放；资源不会复制。</summary>
        public bool TryAssign(BackpackCell cell, CombatCard card)
        {
            if (!_mana.TryGetValue(cell, out var element) || !CanAccept(card, element)) return false;
            _mana.Remove(cell);
            _assigned[card.Id].Add(element);
            return true;
        }

        public bool HasActivationResources(CombatCard card) => card.BaseData.ScanActivatable &&
            _assigned.TryGetValue(card.Id, out var resources) && resources.Count == card.GetSize() &&
            card.GetManaCost().All(cost => resources.Count(e => e.Equals(cost.Key)) == cost.Value);

        /// <summary>只推进扫描时钟；轮次变更不清空、不搬动资源。</summary>
        public bool AdvanceTick(int ticksPerRow = 10)
        {
            if (ticksPerRow < 1) throw new ArgumentOutOfRangeException(nameof(ticksPerRow));
            TicksInRow++;
            if (TicksInRow < ticksPerRow) return false;
            TicksInRow = 0;
            return true;
        }

        /// <summary>完成当前行后调用；返回是否完成了本方一轮。</summary>
        public bool FinishRow()
        {
            Row++;
            if (Row < Height) return false;
            Row = 0;
            Round++;
            return true;
        }

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
            _assigned.Add(card.Id, []);
            return Array.AsReadOnly(replaced);
        }

        internal void RemoveCard(int cardId)
        {
            if (cardId == MainCardId) throw new InvalidOperationException("主功法固定卡不能移除。");
            if (!_placements.Remove(cardId, out var cells)) return;
            foreach (var cell in cells) _cardCells.Remove(cell);
            _assigned.Remove(cardId);
        }
    }
}
