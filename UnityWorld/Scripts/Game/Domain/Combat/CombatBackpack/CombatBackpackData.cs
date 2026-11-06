using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public sealed partial class CombatBackpack
    {
        // 背包状态及只读查询；所有 partial 文件共享这一份状态。
        internal CombatNpc PresentationOwner { get; set; }
        private readonly HashSet<BackpackCell> _validCells;
        private readonly Dictionary<BackpackCell, int> _cardCells = new();
        private readonly Dictionary<int, IReadOnlyList<BackpackCell>> _placements = new();
        private readonly Dictionary<BackpackCell, ElementType> _mana = new();
        public int Width { get; }
        public int Height { get; }
        public int Capacity => _validCells.Count;
        public int MainCardId { get; }
        public int Round { get; private set; } = 1;
        public int Row { get; private set; }
        private decimal _scanProgress;
        /// <summary>以正常速度 Tick 为单位的精确行进度，跨行保留余量。</summary>
        public decimal ScanProgress => _scanProgress;
        /// <summary>旧查询适配；完整精度请读取 ScanProgress。</summary>
        public int TicksInRow => (int)_scanProgress;
        /// <summary>加速剩余真实战斗 Tick，同类只累加持续时间。</summary>
        public int HasteTicks { get; private set; }
        /// <summary>减速剩余真实战斗 Tick，与加速独立计时。</summary>
        public int SlowTicks { get; private set; }
        public IReadOnlyDictionary<BackpackCell, ElementType> Mana { get; }
        public IReadOnlyDictionary<int, IReadOnlyList<BackpackCell>> Placements { get; }

        public bool IsValid(BackpackCell cell) => _validCells.Contains(cell);
        public int? CardAt(BackpackCell cell) => _cardCells.TryGetValue(cell, out var id) ? id : null;
        public bool IsCardCompleteAt(int cardId, BackpackCell cell) => _placements[cardId][^1] == cell;

        public List<BackpackCell> EmptyCells() => _validCells.Where(c => !_cardCells.ContainsKey(c) && !_mana.ContainsKey(c))
            .OrderBy(c => c.Y).ThenBy(c => c.X).ToList();
    }
}
