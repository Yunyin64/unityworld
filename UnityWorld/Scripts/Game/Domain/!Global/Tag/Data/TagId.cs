namespace UnityWorld.Game.Domain.Tag
{
    /// <summary>
    /// Tag 强类型 ID
    /// </summary>
    public readonly struct TagId
    {
        public readonly string Value;

        public TagId(string value)
        {
            Value = value;
        }

        public static readonly TagId Empty = new TagId("");

        public override string ToString() => Value;
    }
}
