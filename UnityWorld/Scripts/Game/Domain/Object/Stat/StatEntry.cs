namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 单个属性条目：持有基础值、修正列表，并计算最终�?
    /// </summary>
    public class StatEntry
    {
        private float _baseValue;
        private readonly List<StatModifier> _modifiers = new();
        private float _cachedFinalValue;
        private bool _isDirty = true;

        public float BaseValue
        {
            get => _baseValue;
            set { _baseValue = value; _isDirty = true; }
        }

        /// <summary>
        /// 最终值（有脏标记时重新计算）
        /// 计算顺序：① Flat加法 �?�?Percent乘法 �?�?Override覆盖 �?�?Clamp夹紧
        /// </summary>
        public float FinalValue
        {
            get
            {
                if (_isDirty) Recalculate();
                return _cachedFinalValue;
            }
        }

        public StatEntry(float baseValue)
        {
            _baseValue = baseValue;
        }

        /// <summary>添加一个修�?/summary>
        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            _isDirty = true;
        }

        /// <summary>移除指定来源的所有修�?/summary>
        public void RemoveModifiersBySource(string sourceId)
        {
            int removed = _modifiers.RemoveAll(m => m.SourceId == sourceId);
            if (removed > 0) _isDirty = true;
        }

        /// <summary>移除所有修�?/summary>
        public void ClearModifiers()
        {
            if (_modifiers.Count == 0) return;
            _modifiers.Clear();
            _isDirty = true;
        }

        private void Recalculate()
        {
            float result = _baseValue;
            float flatSum = 0f;
            float percentSum = 0f;
            bool hasOverride = false;
            float overrideValue = 0f;
            float clampMin = float.MinValue;
            float clampMax = float.MaxValue;

            foreach (var mod in _modifiers)
            {
                switch (mod.Type)
                {
                    case ModifierType.Flat:
                        flatSum += mod.Value;
                        break;
                    case ModifierType.Percent:
                        percentSum += mod.Value;
                        break;
                    case ModifierType.Override:
                        hasOverride = true;
                        overrideValue = mod.Value; // 多个Override取最后一�?
                        break;
                    case ModifierType.ClampMin:
                        clampMin = MathF.Max(clampMin, mod.Value);
                        break;
                    case ModifierType.ClampMax:
                        clampMax = MathF.Min(clampMax, mod.Value);
                        break;
                }
            }

            if (hasOverride)
            {
                result = overrideValue;
            }
            else
            {
                result = (result + flatSum) * (1f + percentSum);
            }

            // 夹紧
            result = MathF.Max(clampMin, MathF.Min(clampMax, result));

            _cachedFinalValue = result;
            _isDirty = false;
        }
    }
}
