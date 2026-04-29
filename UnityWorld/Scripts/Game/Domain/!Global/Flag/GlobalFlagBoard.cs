using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 全局变量黑板：无主体 ID，直接以 string key 存储全局 Flag
    /// 用于跨主体的叙事状态（如"太阳道统是否已现世"）
    /// </summary>
    public class GlobalFlagBoard
    {
        // ── 内部数据 ──────────────────────────────────────────
        private readonly Dictionary<string, object> _flags = new(StringComparer.OrdinalIgnoreCase);

        // ── 写操作 ────────────────────────────────────────────

        /// <summary>设置全局 Flag 值</summary>
        public void Set(string key, object value) => _flags[key] = value;

        /// <summary>移除全局 Flag</summary>
        public void Remove(string key) => _flags.Remove(key);

        /// <summary>清除所有全局 Flag</summary>
        public void ClearAll() => _flags.Clear();

        // ── 读操作 ────────────────────────────────────────────

        /// <summary>获取原始 object 值，不存在返回 null</summary>
        public object? Get(string key) => _flags.TryGetValue(key, out var val) ? val : null;

        /// <summary>尝试以指定类型获取值，失败返回 defaultValue</summary>
        public T GetAs<T>(string key, T defaultValue = default!)
        {
            var raw = Get(key);
            if (raw is T typed) return typed;
            try
            {
                if (raw != null) return (T)Convert.ChangeType(raw, typeof(T));
            }
            catch
            {
                LogMgr.Warn("[GlobalFlagBoard] GetAs<{0}> 转型失败 key={1}", typeof(T).Name, key);
            }
            return defaultValue;
        }

        /// <summary>获取 bool 值（不存在返回 false）</summary>
        public bool GetBool(string key) => GetAs<bool>(key, false);

        /// <summary>获取 int 值（不存在返回 0）</summary>
        public int GetInt(string key) => GetAs<int>(key, 0);

        /// <summary>获取 float 值（不存在返回 0f）</summary>
        public float GetFloat(string key) => GetAs<float>(key, 0f);

        /// <summary>获取 string 值（不存在返回空字符串）</summary>
        public string GetString(string key) => GetAs<string>(key, "");

        /// <summary>检查全局 Flag 是否存在</summary>
        public bool Has(string key) => _flags.ContainsKey(key);

        /// <summary>获取所有全局 Flag（只读视图）</summary>
        public IReadOnlyDictionary<string, object> GetAll() => _flags;
    }
}
