using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public sealed partial class CombatBackpack
    {
        // 背包状态及只读查询；所有 partial 文件共享这一份状态。
        /// <summary>背包所属的回放角色</summary>
        internal CombatNpc PresentationOwner { get; set; }
        /// <summary>本场备战布局副本</summary>
        private readonly NpcBackpackData _layout;
        /// <summary>本场选牌数据副本</summary>
        private readonly NpcCardData _cardData;
        // 仅供所属 CombatNpc 的继承查询使用；不会返回世界数据。
        /// <summary>本场背包布局</summary>
        internal NpcBackpackData LayoutData => _layout;
        /// <summary>本场卡牌与出战选择</summary>
        internal NpcCardData CardData => _cardData;
        /// <summary>背包允许使用的格子</summary>
        private readonly HashSet<BackpackCell> _validCells;
        /// <summary>按占格索引的卡牌编号</summary>
        private readonly Dictionary<BackpackCell, int> _cardCells = new();
        /// <summary>按卡牌编号索引的占格</summary>
        private readonly Dictionary<int, IReadOnlyList<BackpackCell>> _placements = new();
        /// <summary>按所在格索引的盘面灵元</summary>
        private readonly Dictionary<BackpackCell, ElementType> _mana = new();
        /// <summary>背包矩形边界宽度</summary>
        public int Width => _layout.Width;
        /// <summary>背包矩形边界高度</summary>
        public int Height => _layout.Height;
        /// <summary>背包有效格总数</summary>
        public int Capacity => _validCells.Count;
        /// <summary>主功法固定卡编号</summary>
        public int MainCardId => _cardData.MainGongFaCardId!.Value;
        /// <summary>当前扫描轮次</summary>
        public int Round { get; private set; } = 1;
        /// <summary>当前扫描行号</summary>
        public int Row { get; private set; }
        /// <summary>精确累计的行内扫描进度</summary>
        private decimal _scanProgress;
        /// <summary>以正常速度 Tick 为单位的精确行进度，跨行保留余量。</summary>
        public decimal ScanProgress => _scanProgress;
        /// <summary>旧查询适配；完整精度请读取 ScanProgress。</summary>
        public int TicksInRow => (int)_scanProgress;
        /// <summary>加速剩余真实战斗 Tick，同类只累加持续时间。</summary>
        public int HasteTicks { get; private set; }
        /// <summary>减速剩余真实战斗 Tick，与加速独立计时。</summary>
        public int SlowTicks { get; private set; }
        /// <summary>眩晕剩余真实战斗 Tick，期间背包不自然推进。</summary>
        public int StunTicks { get; private set; }
        /// <summary>盘面各格灵元的只读视图</summary>
        public IReadOnlyDictionary<BackpackCell, ElementType> Mana { get; }
        /// <summary>各卡牌占格的只读视图</summary>
        public IReadOnlyDictionary<int, IReadOnlyList<BackpackCell>> Placements { get; }

        /// <summary>判断格子是否属于背包有效范围</summary>
        public bool IsValid(BackpackCell cell) => _validCells.Contains(cell);
        /// <summary>查找占据该格的卡牌编号</summary>
        public int? CardAt(BackpackCell cell) => _cardCells.TryGetValue(cell, out var id) ? id : null;
        /// <summary>判断当前格是否到达本轮可结算的卡牌末格</summary>
        public bool IsCardCompleteAt(int cardId, BackpackCell cell) => _placements[cardId][^1] == cell &&
            (!_movedPastScan.TryGetValue(cardId, out int round) || round != Round);

        // 行边界的事件也必须看到最后访问过的列，到下一行才重置。
        /// <summary>本行已扫描到的列</summary>
        internal int ScannedColumn { get; set; } = -1;
        /// <summary>卡牌越过扫描线时所在的轮次</summary>
        private readonly Dictionary<int, int> _movedPastScan = new();

        /// <summary>取得本场布局的独立值副本，不暴露世界配置或内部可修改集合。</summary>
        public NpcBackpackData GetLayoutSnapshot() => _layout.Clone();
        /// <summary>位置归本场布局，卡自身没有位置字段。</summary>
        public BackpackPosition? GetCardPosition(int cardId) => _layout.GetPosition(cardId);
        /// <summary>取得本场卡的只读占格；历史/已移除卡返回空集合。</summary>
        public IReadOnlyList<BackpackCell> GetCardCells(int cardId) => _placements.TryGetValue(cardId, out var cells)
            ? cells : Array.Empty<BackpackCell>();

        /// <summary>按行列列出未放置卡或灵元的有效格</summary>
        public List<BackpackCell> EmptyCells() => _validCells.Where(c => !_cardCells.ContainsKey(c) && !_mana.ContainsKey(c))
            .OrderBy(c => c.Y).ThenBy(c => c.X).ToList();
    }
}
