using NLua;

namespace UnityWorld.Core
{
    /// <summary>
    /// 通用 Lua 执行环境。路径和绑定对象由调用方提供，不引用游戏数据或玩法类型。
    /// 每个宿主持有自己的实例，避免跨游戏共享全局变量与模块缓存。
    /// </summary>
    public sealed class LuaRuntime : IDisposable
    {
        private Lua _state;

        /// <summary>创建 UTF-8 Lua 环境，启用与原运行时一致的 CLR 互操作。</summary>
        public LuaRuntime()
        {
            _state = new Lua();
            try
            {
                _state.State.Encoding = System.Text.Encoding.UTF8;
                _state.LoadCLRPackage();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>注册调用方提供的全局对象，其用途与生命周期由调用方决定。</summary>
        public void RegisterGlobal(string name, object value)
        {
            EnsureAlive();
            _state[name] = value;
        }

        /// <summary>注册显式提供的枚举类型，保留 C# 枚举值并返回注册数量。</summary>
        public int RegisterEnums(IEnumerable<Type> enumTypes)
        {
            EnsureAlive();
            int count = 0;
            foreach (var enumType in enumTypes)
            {
                if (!enumType.IsEnum)
                    throw new ArgumentException($"{enumType.FullName} 不是枚举类型。", nameof(enumTypes));
                _state.NewTable(enumType.Name);
                var table = _state.GetTable(enumType.Name);
                foreach (var value in Enum.GetValues(enumType)) table[value.ToString()] = value;
                count++;
            }
            return count;
        }

        /// <summary>将指定模块目录加入 require 搜索路径，不固定游戏数据目录。</summary>
        public void AddModuleDirectory(string directory)
        {
            // 按 Lua 字符串转义，兼容包含单引号的宿主目录。
            var luaPath = directory.Replace("\\", "/").Replace("'", "\\'");
            Execute($"package.path = '{luaPath}/?.lua;' .. package.path");
        }

        /// <summary>执行 UTF-8 文件，保留文件名诊断；不缓存返回的可变 table。</summary>
        public object[] ExecuteFile(string filePath)
        {
            EnsureAlive();
            return Execute(File.ReadAllText(filePath, System.Text.Encoding.UTF8), filePath);
        }

        /// <summary>执行代码并返回原始结果，异常交给调用方决定处理策略。</summary>
        public object[] Execute(string code, string chunkName = "chunk")
        {
            EnsureAlive();
            return _state.DoString(code, chunkName);
        }

        /// <summary>扫描 table 的函数成员，不解释 Hook 名称或事件含义。</summary>
        public static Dictionary<string, LuaFunction> ScanFunctions(LuaTable table)
        {
            var functions = new Dictionary<string, LuaFunction>();
            if (table == null) return functions;
            foreach (var key in table.Keys)
                if (key is string name && table[name] is LuaFunction function) functions[name] = function;
            return functions;
        }

        /// <summary>调用函数并返回原始结果，不吞掉异常或执行玩法规则。</summary>
        public static object[] Call(LuaFunction function, params object[] args)
        {
            return function.Call(args);
        }

        /// <summary>读取首个返回值，沿用原 Hook 的类型转换；无返回值时返回默认值。</summary>
        public static T Call<T>(LuaFunction function, params object[] args)
        {
            var results = Call(function, args);
            if (results != null && results.Length > 0 && results[0] is T value) return value;
            if (results != null && results.Length > 0 && results[0] != null)
                return (T)Convert.ChangeType(results[0], typeof(T));
            return default;
        }

        /// <summary>释放本实例的环境，可重复调用；调用方须先清理使用中的绑定。</summary>
        public void Dispose()
        {
            _state?.Dispose();
            _state = null;
        }

        private void EnsureAlive()
        {
            ObjectDisposedException.ThrowIf(_state == null, this);
        }
    }
}
