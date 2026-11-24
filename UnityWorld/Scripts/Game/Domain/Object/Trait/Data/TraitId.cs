namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Trait（特质）的强类型标识符，对应 TraitDefine.Id
    /// </summary>
    public readonly struct TraitId : IEquatable<TraitId>
    {
        /// <summary>特质标识的字符串值</summary>
        public readonly string Value;

        /// <summary>用字符串构造特质标识，空引用按空串处理</summary>
        public TraitId(string value) => Value = value ?? "";

        /// <summary>忽略大小写比较两个特质标识</summary>
        public bool Equals(TraitId other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        /// <summary>判断对象是否为忽略大小写相同的特质标识</summary>
        public override bool Equals(object obj) => obj is TraitId other && Equals(other);
        /// <summary>计算忽略大小写的特质编号哈希</summary>
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? "");
        /// <summary>格式化特质标识及其字符串值</summary>
        public override string ToString() => $"TraitId({Value})";

        /// <summary>忽略大小写判断特质标识相等</summary>
        public static bool operator ==(TraitId a, TraitId b) => a.Equals(b);
        /// <summary>忽略大小写判断特质标识不同</summary>
        public static bool operator !=(TraitId a, TraitId b) => !a.Equals(b);

        /// <summary>将字符串隐式转换为特质标识</summary>
        public static implicit operator TraitId(string id) => new(id);
        /// <summary>取出特质标识的原始字符串以完成隐式转换</summary>
        public static implicit operator string(TraitId id) => id.Value;
    }
}
