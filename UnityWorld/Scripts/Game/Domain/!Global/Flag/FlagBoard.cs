using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 泛型变量黑板：以主体 ID 为 key，维护每个主体的 KV 集合
    /// 支持任意可哈希 ID 类型（int / TileId / PlaneId 等）
    /// Value 类型为 object，可存储 bool / int / float / string
    /// </summary>
    public class FlagBoard<TKey> where TKey : notnull
    {
        // ── 内部数据 ──────────────────────────────────────────
        private readonly Dictionary<TKey, Dictionary<string, object>> _boards = new();

        // ── 写操作 ────────────────────────────────────────────

        /// <summary>设置指定主体的 Flag 值（不存在则创建）</summary>
        public void Set(TKey id, string key, object value)
        {
            if (!_boards.TryGetValue(id, out var dict))
            {
                dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                _boards[id] = dict;
            }
            dict[key] = value;
        }

        /// <summary>移除指定主体的某个 Flag</summary>
        public void Remove(TKey id, string key)
        {
            if (_boards.TryGetValue(id, out var dict))
                dict.Remove(key);
        }

        /// <summary>清除指定主体的所有 Flag</summary>
        public void Clear(TKey id) => _boards.Remove(id);

        /// <summary>清除全部数据</summary>
        public void ClearAll() => _boards.Clear();

        // ── 读操作 ────────────────────────────────────────────

        /// <summary>获取原始 object 值，不存在返回 null</summary>
        public object? Get(TKey id, string key)
        {
            if (_boards.TryGetValue(id, out var dict) && dict.TryGetValue(key, out var val))
                return val;
            return null;
        }

        /// <summary>尝试以指定类型获取值，失败返回 defaultValue</summary>
        public T GetAs<T>(TKey id, string key, T defaultValue = default!)
        {
            var raw = Get(id, key);
            if (raw is T typed) return typed;
            try
            {
                if (raw != null) return (T)Convert.ChangeType(raw, typeof(T));
            }
            catch
            {
                LogMgr.Instance.Warn("[FlagBoard] GetAs<{0}> 转型失败 key={1}", typeof(T).Name, key);
            }
            return defaultValue;
        }

        /// <summary>获取 bool 值（不存在返回 false）</summary>
        public bool GetBool(TKey id, string key) => GetAs<bool>(id, key, false);

        /// <summary>获取 int 值（不存在返回 0）</summary>
        public int GetInt(TKey id, string key) => GetAs<int>(id, key, 0);

        /// <summary>获取 float 值（不存在返回 0f）</summary>
        public float GetFloat(TKey id, string key) => GetAs<float>(id, key, 0f);

        /// <summary>获取 string 值（不存在返回空字符串）</summary>
        public string GetString(TKey id, string key) => GetAs<string>(id, key, "");

        /// <summary>检查指定主体是否存在某 Flag</summary>
        public bool Has(TKey id, string key)
            => _boards.TryGetValue(id, out var dict) && dict.ContainsKey(key);

        /// <summary>获取指定主体的所有 Flag（只读视图）</summary>
        public IReadOnlyDictionary<string, object>? GetAll(TKey id)
            => _boards.TryGetValue(id, out var dict) ? dict : null;
    }
}
