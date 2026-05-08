using NCalc;
using UnityWorld.Game.Data;

namespace UnityWorld.Core
{
    /// <summary>
    /// 属性集合：管理所有 StatEntry，按 StatId（string）索引
    /// 
    /// 特性：
    /// - 惰性创建：无 Entry 时 Get 返回 Define.DefaultValue
    /// - Define 夹紧：Get 时应用 MinValue/MaxValue 硬夹紧
    /// </summary>
    public class StatBlock
    {
        /// <summary>空属性块（只读占位，用于 NPC 尚未注册时）</summary>
        public static readonly StatBlock Empty = new();
        private readonly Dictionary<string, StatEntry> _stats = new();

        private  static Dictionary<string,Expression> _exprCache = new();
        public static Expression GetExpression(string formulaStr)
        {
            if (_exprCache.TryGetValue(formulaStr, out var cachedExpr))
            {
                return cachedExpr;
            }
            else
            {
                var expr = new Expression(formulaStr);
                _exprCache[formulaStr] = expr;
                return expr;
            }
        }
        private readonly Dictionary<string, object?> _finalCache = new();

        public Dictionary<string, object?> GetFinalCache() => _finalCache;
        public void InitType(string Type)
        {
            StatDefineMgr.Instance.GetAll().Where(d => d.Type == Type && d.BaseType == StatBaseType.Primary).ToList().ForEach(d =>
            {
                var entry = GetOrCreateEntry(d.ID);
                if( string.IsNullOrEmpty(d.ExtraBase)) 
                GetFinalCache()[d.ID] = entry.FinalValue; 
            });
        }
        /// <summary>
        /// 获取属性最终值
        /// 
        /// 惰性创建逻辑：
        /// 1. 有 Entry → 计算 entry.FinalValue → 应用 Define 夹紧 → 返回
        /// 2. 无 Entry → 返回 Define.DefaultValue（应用夹紧）或 defaultValue
        /// </summary>
        public float Get(string statId, float defaultValue = 0f)
        {
            float rawValue;
            var define = StatDefineMgr.Instance?.Get(statId);
            if(define == null) return defaultValue;
            
            
            if (_stats.TryGetValue(statId, out var entry))
            {
                // 有 Entry：使用其 FinalValue
                rawValue = entry.FinalValue;
            }
            else
            {
                // 无 Entry：从 Define 读取 DefaultValue
                rawValue = define?.DefaultValue ?? defaultValue;
            }

            // 应用 Define 的 Min/Max 硬夹紧
            return ApplyDefineClamp(statId, rawValue);
        }

        /// <summary>
        /// 永久修改属性基础值（仅 Primary 属性）
        /// 用于升级成长、永久道具、不可逆奖励等场景
        /// Derived 属性请勿调用此方法（其 Base 由公式驱动）
        /// </summary>
        public void AddBase(string statId, float amount)
        {
            var define = StatDefineMgr.Instance?.Get(statId);
            if (define == null) return;
            if (define.BaseType != StatBaseType.Primary)
            {
                LogMgr.Warn("StatBlock", $"尝试对非 Primary 属性 [{statId}] 调用 AddBase，已忽略。Derived 属性请使用公式驱动。");
                return;
            }
            var entry = GetOrCreateEntry(statId);
            entry.Add(amount);
        }

        /// <summary>
        /// 设置属性基础值（仅 Primary 属性）
        /// </summary>
        public void SetBase(string statId, float value)
        {
            var define = StatDefineMgr.Instance?.Get(statId);
            if (define == null) return;
            if (define.BaseType != StatBaseType.Primary)
            {
                LogMgr.Warn("StatBlock", $"尝试对非 Primary 属性 [{statId}] 调用 SetBase，已忽略。Derived 属性请使用公式驱动。");
                return;
            }
            var entry = GetOrCreateEntry(statId);
            entry.SetAdd(value);
        }

        /// <summary>
        /// 添加修正（惰性创建：不存在该属性时自动创建 Entry）
        /// </summary>
        public void AddModifier(string statId, StatModifier modifier)
        {
            var  entry = GetOrCreateEntry(statId);
            entry.AddModifier(modifier);
        }

        /// <summary>
        /// 移除指定来源的所有修正
        /// </summary>
        public void RemoveModifiersBySource(string sourceId)
        {
            foreach (var entry in _stats.Values)
                entry.RemoveModifiersBySource(sourceId);
        }

        /// <summary>
        /// 是否已有该属性的 Entry（不检查 Define 是否存在）
        /// </summary>
        public bool Has(string statId) => _stats.ContainsKey(statId);

        /// <summary>
        /// 获取底层 StatEntry（用于高级操作如 Add/Set，谨慎使用）
        /// 返回 null 表示无 Entry（但不代表该 Stat 不存在，可能只是惰性）
        /// </summary>
        public StatEntry GetEntry(string statId)
        {
            return _stats.TryGetValue(statId, out var entry) ? entry : null;
        }

        /// <summary>
        /// 获取或创建 StatEntry（用于 Add/Set 操作）
        /// </summary>
        public StatEntry GetOrCreateEntry(string statId)
        {
            if (!_stats.TryGetValue(statId, out var entry))
            {
                entry = new StatEntry(statId,this);
                _stats[statId] = entry;
            }
            return entry;
        }

        // ── 私有方法 ─────────────────────────────────────────

        /// <summary>
        /// 应用 Define 的 MinValue/MaxValue 硬夹紧
        /// </summary>
        private static float ApplyDefineClamp(string statId, float value)
        {
            var define = StatDefineMgr.Instance?.Get(statId);
            if (define == null) return value;

            if (define.MinValue.HasValue)
                value = MathF.Max(define.MinValue.Value, value);
            if (define.MaxValue.HasValue)
                value = MathF.Min(define.MaxValue.Value, value);

            return value;
        }

        /// <summary>创建一份独立的 StatBlock 快照（用于战斗等需要隔离的场景）</summary>
    public StatBlock Snapshot(){
    var copy = new StatBlock();
    foreach (var (id, entry) in _stats)
    {
        var newEntry = copy.GetOrCreateEntry(id);
        // 拷贝所有 modifier（StatModifier 是 struct，自动值拷贝）
        foreach (var mod in entry.GetModifiers())  newEntry.AddModifier(mod);
        newEntry.SetAdd(entry.AddValue);
    }
    return copy;
}
    }
}