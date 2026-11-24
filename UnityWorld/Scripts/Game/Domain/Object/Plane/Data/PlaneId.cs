namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 位面唯一ID（值对象）
    /// </summary>
    public readonly struct PlaneId : IEquatable<PlaneId>
    {
        /// <summary>位面标识的整数值</summary>
        public readonly int Value;

        /// <summary>用整数构造位面标识</summary>
        public PlaneId(int value) => Value = value;

        /// <summary>按整数值比较两个位面标识</summary>
        public bool Equals(PlaneId other) => Value == other.Value;
        /// <summary>判断对象是否为整数值相同的位面标识</summary>
        public override bool Equals(object obj) => obj is PlaneId other && Equals(other);
        /// <summary>用位面标识的整数值作哈希</summary>
        public override int GetHashCode() => Value;
        /// <summary>格式化位面标识及其整数值</summary>
        public override string ToString() => $"PlaneId({Value})";

        /// <summary>判断两个位面标识的整数值相等</summary>
        public static bool operator ==(PlaneId a, PlaneId b) => a.Value == b.Value;
        /// <summary>判断两个位面标识的整数值不同</summary>
        public static bool operator !=(PlaneId a, PlaneId b) => a.Value != b.Value;

        // int -> PlaneId（可以直接用 int 赋值给 PlaneId）
        /// <summary>将整数隐式转换为位面标识</summary>
        public static implicit operator PlaneId(int value) => new PlaneId(value);

        // PlaneId -> int（可以直接用 PlaneId 赋值给 int，或当 int 使用）
        /// <summary>读取位面标识的整数值以完成隐式转换</summary>
        public static implicit operator int(PlaneId id) => id.Value;

    }

    /// <summary>
    /// 位面ID生成器（线程安全自增�?
    /// </summary>
    public static class PlaneIdGenerator
    {
        /// <summary>位面编号的递增计数器</summary>
        private static int _next = 1;

        /// <summary>原子递增计数并返回新位面标识</summary>
        public static PlaneId Generate() => new PlaneId(System.Threading.Interlocked.Increment(ref _next));

        /// <summary>重置计数器（仅用于测试）</summary>
        public static void Reset() => _next = 1;
    }
}
