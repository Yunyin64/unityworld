namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Trait（特质）的强类型标识符，对应 TraitDefine.Id
    /// </summary>
    public readonly struct TraitId : IEquatable<TraitId>
    {
        public readonly string Value;

        public TraitId(string value) => Value = value ?? "";

        public bool Equals(TraitId other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object? obj) => obj is TraitId other && Equals(other);
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? "");
        public override string ToString() => $"TraitId({Value})";

        public static bool operator ==(TraitId a, TraitId b) => a.Equals(b);
        public static bool operator !=(TraitId a, TraitId b) => !a.Equals(b);

        public static implicit operator TraitId(string id) => new(id);
        public static implicit operator string(TraitId id) => id.Value;
    }
}
