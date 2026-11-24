namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 六边形地块的唯一坐标ID（Axial 坐标系）
    /// q 为列轴，r 为行轴；第三�?s = -q - r（满�?q+r+s=0�?
    /// </summary>
    public readonly struct TileId : IEquatable<TileId>
    {
        /// <summary>列轴</summary>
        public readonly int Q;

        /// <summary>行轴</summary>
        public readonly int R;

        /// <summary>用轴坐标的两个分量创建六边形地块编号</summary>
        public TileId(int q, int r) { Q = q; R = r; }

        /// <summary>第三轴（满足 q+r+s=0）</summary>
        public int S => -Q - R;

        // ── 六个邻居方向（尖顶六边形 pointy-top，odd-r 布局）────
        // 顺时针：右、右下、左下、左、左上、右上
        // Axial 偏移与 odd-r offset 下的视觉方向一致
        /// <summary>按固定顺序排列的六个邻向</summary>
        public static readonly TileId[] Directions =
        {
            new( 1,  0),   // 0: 右
            new( 0,  1),   // 1: 右下
            new(-1,  1),   // 2: 左下
            new(-1,  0),   // 3: 左
            new( 0, -1),   // 4: 左上
            new( 1, -1),   // 5: 右上
        };

        /// <summary>获取指定方向的邻居坐标（0~5，顺时针从「右」开始）</summary>
        public TileId Neighbor(int direction) => this + Directions[((direction % 6) + 6) % 6];

        /// <summary>获取所有六个邻居坐标</summary>
        public TileId[] Neighbors()
        {
            var result = new TileId[6];
            for (int i = 0; i < 6; i++) result[i] = Neighbor(i);
            return result;
        }

        /// <summary>到另一个坐标的六边形距离</summary>
        public int DistanceTo(TileId other)
            => (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;

        // ── 运算�?────────────────────────────────────────
        /// <summary>将两个轴坐标逐分量相加</summary>
        public static TileId operator +(TileId a, TileId b) => new(a.Q + b.Q, a.R + b.R);
        /// <summary>求两个轴坐标各分量的差</summary>
        public static TileId operator -(TileId a, TileId b) => new(a.Q - b.Q, a.R - b.R);

        /// <summary>比较两个地块编号的轴坐标</summary>
        public bool Equals(TileId other) => Q == other.Q && R == other.R;
        /// <summary>判断对象是否为同一轴坐标的地块编号</summary>
        public override bool Equals(object obj) => obj is TileId other && Equals(other);
        /// <summary>用两个轴坐标分量组合哈希</summary>
        public override int GetHashCode() => HashCode.Combine(Q, R);
        /// <summary>格式化地块的两个轴坐标分量</summary>
        public override string ToString() => $"TileId({Q},{R})";

        /// <summary>判断两个地块编号的坐标相同</summary>
        public static bool operator ==(TileId a, TileId b) => a.Equals(b);
        /// <summary>判断两个地块编号的坐标不同</summary>
        public static bool operator !=(TileId a, TileId b) => !a.Equals(b);

        /// <summary>原点</summary>
        public static readonly TileId Zero = new(0, 0);
    }
}
