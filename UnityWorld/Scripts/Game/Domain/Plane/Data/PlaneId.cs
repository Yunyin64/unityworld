namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 位面唯一ID（值对象）
    /// </summary>
    public readonly struct PlaneId : IEquatable<PlaneId>
    {
        public readonly int Value;

        public PlaneId(int value) => Value = value;

        public bool Equals(PlaneId other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is PlaneId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"PlaneId({Value})";

        public static bool operator ==(PlaneId a, PlaneId b) => a.Value == b.Value;
        public static bool operator !=(PlaneId a, PlaneId b) => a.Value != b.Value;

        // int -> PlaneId（可以直接用 int 赋值给 PlaneId）
        public static implicit operator PlaneId(int value) => new PlaneId(value);

        // PlaneId -> int（可以直接用 PlaneId 赋值给 int，或当 int 使用）
        public static implicit operator int(PlaneId id) => id.Value;

    }

    /// <summary>
    /// 位面ID生成器（线程安全自增�?
    /// </summary>
    public static class PlaneIdGenerator
    {
        private static int _next = 1;

        public static PlaneId Generate() => new PlaneId(System.Threading.Interlocked.Increment(ref _next));

        /// <summary>重置计数器（仅用于测试）</summary>
        public static void Reset() => _next = 1;
    }
}
