namespace UnityWorld.Game.Domain.Tag
{
    /// <summary>
    /// Tag 强类型 ID
    /// </summary>
    public readonly struct TagId
    {
        /// <summary>标签标识的原始字符串</summary>
        public readonly string Value;

        /// <summary>用字符串创建标签标识</summary>
        public TagId(string value)
        {
            Value = value;
        }

        /// <summary>表示空标签的标识</summary>
        public static readonly TagId Empty = new TagId("");

        /// <summary>返回标签标识的原始字符串</summary>
        public override string ToString() => Value;
    }
}
