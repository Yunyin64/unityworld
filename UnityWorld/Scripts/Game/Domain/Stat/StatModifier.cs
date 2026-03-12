namespace UnityWorld.Game.Domain
{
    

    /// <summary>
    /// 属性修正项（来自buff/装备/功法/法宝等）
    /// </summary>
    public struct StatModifier
    {
        /// <summary>修正类型</summary>
        public ModifierType Type;

        /// <summary>修正�?/summary>
        public float Value;

        /// <summary>来源标识（用于精准移除，如buff消失时移除对应modifier�?/summary>
        public string SourceId;

        public StatModifier(ModifierType type, float value, string sourceId = "")
        {
            Type = type;
            Value = value;
            SourceId = sourceId;
        }

        public static StatModifier Flat(float value, string sourceId = "") =>
            new StatModifier(ModifierType.Flat, value, sourceId);

        public static StatModifier Percent(float value, string sourceId = "") =>
            new StatModifier(ModifierType.Percent, value, sourceId);

        public static StatModifier Override(float value, string sourceId = "") =>
            new StatModifier(ModifierType.Override, value, sourceId);

        public static StatModifier ClampMax(float value, string sourceId = "") =>
            new StatModifier(ModifierType.ClampMax, value, sourceId);

        public static StatModifier ClampMin(float value, string sourceId = "") =>
            new StatModifier(ModifierType.ClampMin, value, sourceId);
    }
}
