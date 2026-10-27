namespace UnityWorld.Game.Data
{
    /// <summary>背包坐标，从左上角 (0, 0) 起，X 向右、Y 向下。</summary>
    public readonly record struct BackpackCell(int X, int Y);

    /// <summary>公开的卡实例锚点，从左上角 (1, 1) 起。与内部几何坐标分离。</summary>
    public readonly record struct BackpackPosition(int X, int Y)
    {
        public override string ToString() => $"({X},{Y})";
    }

    public static class BackpackCoordinates
    {
        public static BackpackCell ToCell(BackpackPosition position)
        {
            if (position.X < 1 || position.Y < 1)
                throw new InvalidOperationException($"背包公开坐标必须从 (1,1) 起：{position}。");
            return new(position.X - 1, position.Y - 1);
        }
        public static BackpackPosition ToPosition(BackpackCell cell) => new(cell.X + 1, cell.Y + 1);
        public static List<BackpackCell> HorizontalShape(int size)
        {
            if (size < 1) throw new InvalidOperationException("卡牌 Size 必须为正数，旧零格定义需要明确迁移。");
            return Enumerable.Range(0, size).Select(x => new BackpackCell(x, 0)).ToList();
        }
    }

    /// <summary>主功法某一级的空间及主卡固定位置；空 ValidCells 表示完整矩形。</summary>
    public class BackpackLevelDefine
    {
        public int Level { get; set; } = 1;
        public int Width { get; set; }
        public int Height { get; set; }
        public List<BackpackCell> ValidCells { get; set; } = [];
        public List<BackpackCell> MainCardCells { get; set; } = [];

        /// <summary>解析并校验有效空间；返回独立集合，不能修改原始定义。</summary>
        public HashSet<BackpackCell> GetValidCells()
        {
            if (Level < 1 || Width < 1 || Height < 1)
                throw new InvalidOperationException("背包等级、宽、高必须为正数。");
            var cells = ValidCells.Count == 0
                ? Enumerable.Range(0, Height).SelectMany(y => Enumerable.Range(0, Width).Select(x => new BackpackCell(x, y))).ToList()
                : ValidCells;
            if (cells.Distinct().Count() != cells.Count || cells.Any(c => c.X < 0 || c.Y < 0 || c.X >= Width || c.Y >= Height))
                throw new InvalidOperationException("背包有效格重复或越界。");
            if (MainCardCells.Count == 0 || MainCardCells.Distinct().Count() != MainCardCells.Count || MainCardCells.Any(c => !cells.Contains(c)))
                throw new InvalidOperationException("主功法固定格必须非空、不重复且位于有效空间内。");
            return cells.ToHashSet();
        }

        public BackpackLevelDefine Clone() => new()
        {
            Level = Level, Width = Width, Height = Height,
            ValidCells = [.. ValidCells], MainCardCells = [.. MainCardCells]
        };
    }
}
