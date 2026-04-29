using System.Collections;
using System.Reflection;
using UnityWorld.Game.Data;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// API 函数注册表管理器：注册所有可用的 Action 函数签名，
    /// 提供查询、校验、执行能力。
    /// 是 ActionDefine.Params 到运行时 ContextBase 的桥梁，
    /// 同时通过 [APIFunc] Attribute 反射扫描注册执行委托。
    /// </summary>
    public class APIMgr : IDomainMgrBase
    {
        public static APIMgr? Instance { get; private set; }
        public string Name => "APIMgr";
        public string Desc => "API函数注册表：管理所有Action函数签名定义与执行委托";

        // ── 存储 ──────────────────────────────────────────────

        private readonly Dictionary<string, API> _apis = new();

        /// <summary>执行委托字典：funcName → 执行函数</summary>
        private readonly Dictionary<string, Action<ContextBase>> _handlers = new();

        // ── 构造 ──────────────────────────────────────────────

        public APIMgr()
        {
            Instance = this;
        }

        // ── 注册 ──────────────────────────────────────────────

        /// <summary>
        /// 注册一个 API 函数定义
        /// </summary>
        public void Register(API api)
        {
            _apis[api.FuncName] = api;
        }

        // ── 查询 ──────────────────────────────────────────────

        /// <summary>
        /// 按函数名查询 API 定义，不存在返回 null
        /// </summary>
        public API? Get(string funcName)
        {
            return _apis.TryGetValue(funcName, out var api) ? api : null;
        }

        /// <summary>
        /// 判断函数名是否已注册
        /// </summary>
        public bool Contains(string funcName)
        {
            return _apis.ContainsKey(funcName);
        }

        /// <summary>
        /// 获取所有已注册的 API
        /// </summary>
        public IEnumerable<API> GetAll() => _apis.Values;

        // ── 校验 ──────────────────────────────────────────────

        /// <summary>
        /// 校验 ActionDefine 的 params 是否匹配注册的函数签名。
        /// 返回错误信息，合法时返回空字符串。
        /// </summary>
        public string Validate(string funcName, List<object>? paramValues)
        {
            if (!_apis.TryGetValue(funcName, out var api))
                return $"未注册的函数: {funcName}";

            int valueCount = paramValues?.Count ?? 0;
            if (valueCount < api.RequiredParamCount || valueCount > api.ParamCount)
                return $"函数 {funcName} 需要 {api.RequiredParamCount}~{api.ParamCount} 个参数，实际传入 {valueCount} 个";

            return "";
        }

        /// <summary>
        /// 将 ActionDefine 的 params 列表按注册的函数签名解析到 ContextBase 中。
        /// 按 ParamsList 顺序，将 params[i] 以 ParamName 为 key 写入 context。
        /// </summary>
        public ContextBase? ParseToContext(string funcName, List<object>? paramValues)
        {
            if (!_apis.TryGetValue(funcName, out var api)) return null;

            var ctx = new ContextBase();
            int count = Math.Min(api.ParamCount, paramValues?.Count ?? 0);
            for (int i = 0; i < count; i++)
            {
                string paramName = api.ParamsList[i].Name;
                object raw = paramValues![i];
                ctx.Set(paramName, raw);
            }
            return ctx;
        }

        // ── 执行 ──────────────────────────────────────────────

        /// <summary>
        /// 执行指定函数名的 Handler。
        /// 未注册时打 Warning 跳过，Handler 内部异常时 catch + Warning。
        /// </summary>
        public void Execute(string funcName, ContextBase ctx)
        {
            if (!_handlers.TryGetValue(funcName, out var handler))
            {
                LogMgr.Warn("[APIMgr] Execute: 未注册的 Handler '{0}'，已跳过", funcName);
                return;
            }

            try
            {
                handler(ctx);
            }
            catch (Exception e)
            {
                LogMgr.Warn("[APIMgr] Execute: 执行 '{0}' 时异常：{1}", funcName, e.Message);
            }
        }

        // ── 反射扫描 ──────────────────────────────────────────

        /// <summary>
        /// 扫描当前 Assembly 中所有带 [APIFunc] 的静态方法，
        /// 校验签名为 static void Xxx(ContextBase ctx)，
        /// 同时从 Attribute 的 ParamDefs 解析 API 签名定义，
        /// 一次性注册到 _handlers 和 _apis。
        /// </summary>
        private void ScanHandlers()
        {
            var assembly = Assembly.GetExecutingAssembly();
            int count = 0;

            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attr = method.GetCustomAttribute<APIFuncAttribute>();
                    if (attr == null) continue;

                    // 校验方法签名：返回 void，恰好 1 个参数且类型为 ContextBase
                    var parameters = method.GetParameters();
                    if (method.ReturnType != typeof(void) || parameters.Length != 1 || parameters[0].ParameterType != typeof(ContextBase))
                    {
                        LogMgr.Warn("[APIMgr] ScanHandlers: 方法 {0}.{1} 签名不匹配（需要 static void Xxx(ContextBase ctx)），已跳过",
                            type.Name, method.Name);
                        continue;
                    }

                    if (_handlers.ContainsKey(attr.FuncName))
                    {
                        LogMgr.Warn("[APIMgr] ScanHandlers: FuncName '{0}' 重复注册（{1}.{2} 覆盖前者）",
                            attr.FuncName, type.Name, method.Name);
                    }

                    // 注册 Handler
                    var del = (Action<ContextBase>)Delegate.CreateDelegate(typeof(Action<ContextBase>), method);
                    _handlers[attr.FuncName] = del;

                    // 从 Attribute.ParamDefs 解析并注册 API 签名
                    var api = ParseApiFromAttribute(attr);
                    if (api != null)
                    {
                        api.Method = method; 
                        _apis[api.FuncName] = api;
                    }

                    count++;
                }
            }

            LogMgr.Dbg("[APIMgr] ScanHandlers 完成，已注册 {0} 个 Handler", count);
        }

        /// <summary>
        /// 从 APIFuncAttribute 的 ParamDefs 字符串数组解析出 API 签名定义。
        /// 格式：每个元素为 "ParamName:Type" 或 "?ParamName:Type"（可选参数）。
        /// </summary>
        private static API ParseApiFromAttribute(APIFuncAttribute attr)
        {
            var paramsList = new List<Param>();

            foreach (string raw in attr.ParamDefs)
            {
                bool isOptional = raw.StartsWith("?");
                string cleaned = isOptional ? raw.Substring(1) : raw;

                int colonIdx = cleaned.IndexOf(':');
                if (colonIdx <= 0 || colonIdx >= cleaned.Length - 1)
                {
                    LogMgr.Warn("[APIMgr] ParseApiFromAttribute: FuncName '{0}' 的参数 '{1}' 格式非法（应为 Name:Type），已跳过该参数",
                        attr.FuncName, raw);
                    continue;
                }

                string paramName = cleaned.Substring(0, colonIdx);
                string typeStr = cleaned.Substring(colonIdx + 1);

                if (!Enum.TryParse<Param_TYPE>(typeStr, true, out var paramType))
                {
                    LogMgr.Warn("[APIMgr] ParseApiFromAttribute: FuncName '{0}' 的参数类型 '{1}' 无法识别（支持 Int/Float/String/Bool），已跳过",
                        attr.FuncName, typeStr);
                    continue;
                }

                paramsList.Add(new Param { Type = paramType, Name = paramName, IsOptional = isOptional });
            }

            return new API(attr.FuncName, attr.Desc, paramsList);
        }

        // ── 生命周期 ──────────────────────────────────────────

        /// <summary>
        /// 初始化：反射扫描注册所有 [APIFunc] 标记的 Handler 和 API 签名
        /// </summary>
        public void Init()
        {
            ScanHandlers();
            LogMgr.Dbg("[APIMgr] 初始化完成，已注册 {0} 个API签名，{1} 个Handler", _apis.Count, _handlers.Count);
        }

        public void Begin() { }
        public void Tick(float deltaTime) { }
        public void Update() { }
        public void Render(float dt) { }

        public void End()
        {
            _apis.Clear();
            _handlers.Clear();
            Instance = null;
        }

        public IEnumerator Save() { yield break; }
        public IEnumerator Load() { yield break; }

        public void Log()
        {
            LogMgr.Dbg("=== {0} ===  {1}", Name, Desc);
            LogMgr.Dbg("  已注册API签名数量: {0}", _apis.Count);
            foreach (var api in _apis.Values)
            {
                string hasHandler = _handlers.ContainsKey(api.FuncName) ? " ✓Handler" : " ✗NoHandler";
                LogMgr.Dbg("    {0}{1}", api, hasHandler);
            }
            LogMgr.Dbg("  已注册Handler数量: {0}", _handlers.Count);
            // 输出有 Handler 但无签名的函数
            foreach (var funcName in _handlers.Keys)
            {
                if (!_apis.ContainsKey(funcName))
                {
                    LogMgr.Dbg("    Handler-only: {0}", funcName);
                }
            }
        }

    }
}