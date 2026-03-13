using System;

namespace UnityWorld.Core
{
    /// <summary>
    /// 事件广播的 Scope 实例键，唯一标识一个"哪个对象的哪种 scope"。
    /// Global scope 时 Id 为空字符串。
    /// </summary>
    public readonly struct ScopeKey : IEquatable<ScopeKey>
    {
        /// <summary>Scope 层级类型</summary>
        public EventScope Scope { get; }

        /// <summary>
        /// 对象 ID（字符串形式）。Global scope 时为 <see cref="string.Empty"/>。
        /// </summary>
        public string Id { get; }

        /// <summary>创建一个 ScopeKey</summary>
        public ScopeKey(EventScope scope, string id)
        {
            Scope = scope;
            Id = id ?? string.Empty;
        }

        /// <summary>全局 Scope 的标准 Key（Id 为空字符串）</summary>
        public static ScopeKey Global => new ScopeKey(EventScope.Global, string.Empty);

        // ── 相等性 ─────────────────────────────────────────

        /// <inheritdoc/>
        public bool Equals(ScopeKey other) =>
            Scope == other.Scope && string.Equals(Id, other.Id, StringComparison.Ordinal);

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            obj is ScopeKey other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine((int)Scope, Id);

        /// <summary>相等运算符</summary>
        public static bool operator ==(ScopeKey left, ScopeKey right) => left.Equals(right);

        /// <summary>不等运算符</summary>
        public static bool operator !=(ScopeKey left, ScopeKey right) => !left.Equals(right);

        /// <inheritdoc/>
        public override string ToString() =>
            Scope == EventScope.Global ? "Global" : $"{Scope}[{Id}]";
    }
}
