using UnityWorld.Core;

public class ContextBase
        {
            protected readonly Dictionary<string, object> _causes = new();

            /// <summary>设置一个因果项</summary>
            public ContextBase Set<T>(string key, T value)
            {
                _causes[key] = value;
                return this;
            }

            public ContextBase Set<T>(string key, Func<T> factory,Func<T> factory2 = null)
            {
                if (!_causes.ContainsKey(key))
                {
                    _causes[key] = factory();
                }
                else
                {
                    if (factory2 != null)
                    {
                        _causes[key] = factory2();
                    }
                }
                return this;
            }

            /// <summary>获取一个因果项（引用类型），不存在返回 null</summary>
            public T Get<T>(string key) where T : class
                => _causes.TryGetValue(key, out var v) ? v as T : null;

            /// <summary>获取一个因果项（非泛型，Lua 友好），不存在返回 null</summary>
            public object GetObject(string key)
                => _causes.TryGetValue(key, out var v) ? v : null;

            /// <summary>获取一个因果项（值类型），不存在返回 defaultValue。支持 Lua number（double/long）到 int/float 等的自动转换</summary>
            public T GetValue<T>(string key, T defaultValue = default) where T : struct
            {
                if (!_causes.TryGetValue(key, out var v)) return defaultValue;
                if (v is T t) return t;
                // Lua number → C# int/float/double/long 兼容转换
                try { return (T)System.Convert.ChangeType(v, typeof(T)); }
                catch { return defaultValue; }
            }

            /// <summary>获取一个因果项（字符串），不存在返回 defaultValue</summary>
            public string GetValue(string key, string defaultValue = "")
                => _causes.TryGetValue(key, out var v) && v is string s ? s : defaultValue;

                
            public T GetEmValue<T>(string key, T defaultValue = default) where T : Enum
                => _causes.TryGetValue(key, out var v) && v is T t ? t : defaultValue;

            /// <summary>是否存在指定因果项</summary>
            public bool Has(string key) => _causes.ContainsKey(key);

            /// <summary>获取所有因果项的键（调试用）</summary>
            public IEnumerable<string> Keys => _causes.Keys;

            public virtual string LogAllInfo()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("ContextBase:");
                foreach (var kvp in _causes)
                {
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }

                LogMgr.Dbg(sb.ToString());
                return sb.ToString();
            }
        }