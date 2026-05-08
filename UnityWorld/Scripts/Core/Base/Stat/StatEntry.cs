using NCalc;
using UnityWorld.Game.Data;

namespace UnityWorld.Core
{
    /// <summary>
    /// 单个属性条目：持有 statId、累加值、修正列表，并计算最终值
    /// 
    /// 三层值来源计算公式：
    /// Final = ((Define.DefaultValue + FlatSum) × (1 + PercentSum) + AddValue) → Override → Modifier Clamp → Define Min/Max
    /// </summary>
    public class StatEntry
    {
        // ── 字段 ─────────────────────────────────────────────
        private readonly  StatBlock _ownerBlock;
        private readonly string _statId;
        private float _addValue;
        private  List<StatModifier> _modifiers = new();
        private float _cachedFinalValue => (float)_ownerBlock.GetFinalCache()[_statId];
        private bool _isDirty = true;

        // ── 属性 ─────────────────────────────────────────────

        /// <summary>属性 ID（用于查询 Define）</summary>
        public string StatId => _statId;

        /// <summary>累加账本值（支持财富、声望等累加型属性）</summary>
        public float AddValue
        {
            get => _addValue;
            set { _addValue = value; _isDirty = true; }
        }

        /// <summary>
        /// 最终值（有脏标记时重新计算）
        /// 计算顺序：
        ///   ① Define.DefaultValue 作为 base
        ///   ② (base + FlatSum) × (1 + PercentSum)
        ///   ③ + AddValue
        ///   ④ Override（如果存在）
        ///   ⑤ Modifier 的 ClampMin/ClampMax
        ///   ⑥ Define 的 MinValue/MaxValue（在 StatBlock.Get 中应用）
        /// </summary>
        public float FinalValue
        {
            get
            {
                if (_isDirty) Recalculate();
                return _cachedFinalValue;
            }
        }

        // ── 构造 ────────────────────────────────────────────

        public StatEntry(string statId, StatBlock ownerBlock)
        {
            _statId = statId;
            _ownerBlock = ownerBlock;
            _addValue = 0f;
        }
        public List<StatModifier> GetModifiers() => _modifiers;

        // ── AddValue 操作 ───────────────────────────────────

        /// <summary>累加值（如财富+100、声望+10）</summary>
        public void Add(float amount)
        {
            _addValue += amount;
            _isDirty = true;
        }

        /// <summary>设置累加值</summary>
        public void SetAdd(float value)
        {
            _addValue = value;
            _isDirty = true;
        }

        // ── Modifier 操作 ───────────────────────────────────

        /// <summary>添加一个修正</summary>
        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            _isDirty = true;
        }

        /// <summary>移除指定来源的所有修正</summary>
        public void RemoveModifiersBySource(string sourceId)
        {
            int removed = _modifiers.RemoveAll(m => m.SourceId == sourceId);
            if (removed > 0) _isDirty = true;
        }

        /// <summary>移除所有修正</summary>
        public void ClearModifiers()
        {
            if (_modifiers.Count == 0) return;
            _modifiers.Clear();
            _isDirty = true;
        }

        // ── 计算 ────────────────────────────────────────────

        private void Recalculate()
        {
            // ① 从 Define 读取 DefaultValue
            var define = StatDefineMgr.Instance?.Get(_statId);
            float baseValue = define?.DefaultValue ?? 0f;
            var formulaStr = define.ExtraBase;
            if (!string.IsNullOrEmpty(formulaStr))
            {
                var expr = StatBlock.GetExpression(formulaStr);
                expr.Parameters = _ownerBlock.GetFinalCache();
                baseValue = Convert.ToSingle(expr.Evaluate()) + baseValue;
            }

            // 累积 Modifier
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
                        overrideValue = mod.Value; // 多个 Override 取最后一个
                        break;
                    case ModifierType.ClampMin:
                        clampMin = MathF.Max(clampMin, mod.Value);
                        break;
                    case ModifierType.ClampMax:
                        clampMax = MathF.Min(clampMax, mod.Value);
                        break;
                }
            }

            // ② (base + FlatSum) × (1 + PercentSum)
            float result = (baseValue + flatSum) * (1f + percentSum);

            // ③ + AddValue
            result += _addValue;

            // ④ Override（在 AddValue 之后！）
            if (hasOverride)
            {
                result = overrideValue;
            }

            // ⑤ Modifier 的 ClampMin/ClampMax
            result = MathF.Max(clampMin, MathF.Min(clampMax, result));

            // ⑥ Define 的 MinValue/MaxValue 硬夹紧在 StatBlock.Get 中应用
            _isDirty = false;
            _ownerBlock.GetFinalCache()[_statId] = result; // 更新缓存
        }
    }
}