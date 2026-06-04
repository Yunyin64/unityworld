using System.Collections;
using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Lua 运行时管理器：管理 NLua State 生命周期、
    /// 卡牌脚本加载与 Hook 函数发现。
    /// </summary>
    public class LuaMgr : IDomainMgrBase
    {
        public static LuaMgr Instance { get; private set; }
        public string Name => "LuaMgr";
        public string Desc => "Lua运行时管理器：NLua State 生命周期、脚本加载";

        // ── Lua State ────────────────────────────────────────────

        /// <summary>全局 Lua State（整个应用共享一个）</summary>
        private Lua _luaState;

        /// <summary>Lua 卡牌脚本目录路径</summary>
        private readonly string _luaCardsDir;

        /// <summary>Lua 战斗 Modifier 脚本目录路径</summary>
        private readonly string _luaCombatModifiersDir;

        /// <summary>Lua 初始化脚本路径</summary>
        private readonly string _luaInitPath;

        // ── Hook 函数名 → EventId 映射表 ────────────────────────

        /// <summary>
        /// 约定的 Hook 函数名到 EventMgr 事件 ID 的映射。
        /// OnUse 不在此表中（由框架直接调用，不注册事件）。
        /// </summary>
        public static readonly Dictionary<string, string> HookToEventId = new();

        /// <summary>Keyword 注册表：keyword 名称 → 对应 Lua table</summary>
        private Dictionary<string, LuaTable> _keywordRegistry = new();

        /// <summary>Keyword 脚本目录路径</summary>
        private readonly string _luaKeywordsDir;

        // ── 构造 ────────────────────────────────────────────────

        /// <summary>
        /// 构造 LuaMgr。
        /// </summary>
        /// <param name="luaCardsDir">Lua 卡牌脚本目录（默认 Data/LuaCards）</param>
        public LuaMgr()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _luaCardsDir =  Path.Combine(baseDir, "Data", "Card","Lua");
            _luaCombatModifiersDir = Path.Combine(baseDir, "Data", "Modifier","Lua");
            _luaKeywordsDir = Path.Combine(baseDir, "Data", "LuaScripts", "Keywords");
            _luaInitPath = Path.Combine(baseDir, "Data", "LuaScripts", "Init.lua");
            Instance = this;
        }

        // ── 生命周期 ────────────────────────────────────────────

        /// <summary>
        /// 初始化：创建 Lua State，加载 Init.lua，执行 Keyword 脚本。
        /// </summary>
        public void Init()
        {
            _luaState = new Lua();
            _luaState.LoadCLRPackage();

            // 加载 Init.lua（定义 CardBase、Attack 等全局函数）
            LoadInitScript();

            // 扫描并执行 Keywords/ 目录下所有 Lua 脚本（脚本内自注册）
            LoadKeywordScripts();

            LogMgr.Instance.Dbg("[LuaMgr] 初始化完成，Lua State 已创建");
        }

        public void Begin()
        {
            var triggers = TriggerDefineMgr.Instance.GetAll().ToList();
            foreach (var trigger in triggers)
            {
                var id = trigger.ID;
                var funcname = id.Replace("trigger_", "");
                HookToEventId.Add(funcname, id);
            }
        }
        public void Tick(float deltaTime) { }
        public void Update() { }
        public void Render(float dt) { }

        /// <summary>
        /// 销毁：Dispose Lua State。
        /// </summary>
        public void End()
        {
            _keywordRegistry.Clear();
            _luaState?.Dispose();
            _luaState = null;
            Instance = null;
        }

        public IEnumerator Save() { yield break; }
        public IEnumerator Load() { yield break; }

        public void Log()
        {
            LogMgr.Instance.Dbg("=== {0} ===  {1}", Name, Desc);
            LogMgr.Instance.Dbg("  Lua State: {0}", _luaState != null ? "Active" : "Null");
        }

        // ══════════════════════════════════════════════════════════
        //  Init.lua 加载
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 加载 Data/LuaScripts/Init.lua，注册全局函数和 CardBase。
        /// </summary>
        private void LoadInitScript()
        {
            if (!File.Exists(_luaInitPath))
            {
                LogMgr.Instance.Err("[LuaMgr] Init.lua 不存在: {0}", _luaInitPath);
                return;
            }

            try
            {
                RegisterGlobals();
                RegisterEnumTables();

                // 设置 package.path，让 require 能找到 Data/LuaScripts/ 下的模块
                var luaScriptsDir = Path.GetDirectoryName(_luaInitPath).Replace("\\", "/");
                _luaState.DoString($"package.path = '{luaScriptsDir}/?.lua;' .. package.path");

                _luaState.DoFile(_luaInitPath);
                LogMgr.Instance.Dbg("[LuaMgr] Init.lua 加载成功");
            }
            catch (Exception ex)
            {
                LogMgr.Instance.Err("[LuaMgr] Init.lua 加载失败: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 注入 C# 单例到 Lua 全局空间，供 Lua 脚本直接使用。
        /// 新增全局变量在此处统一注册。
        /// </summary>
        private void RegisterGlobals()
        {
            _luaState["LuaMgr"] = this;
            _luaState["LogMgr"] = LogMgr.Instance;
            _luaState["API"] = APIMgr.Instance;
        }

        /// <summary>
        /// 反射扫描当前程序集所有 public enum，注册为 Lua 全局 table。
        /// 如 CombatCardPhase.Ready、DamageType.Ci 等，Lua 侧直接用。
        /// </summary>
        private void RegisterEnumTables()
        {
            var assembly = typeof(LuaMgr).Assembly;
            var enumTypes = assembly.GetTypes()
                .Where(t => t.IsEnum && t.IsPublic);

            foreach (var enumType in enumTypes)
            {
                var name = enumType.Name;
                _luaState.NewTable(name);
                var tbl = _luaState.GetTable(name);
                foreach (var val in Enum.GetValues(enumType))
                {
                    tbl[val.ToString()] = val;
                }
            }

            LogMgr.Instance.Dbg("[LuaMgr] 枚举注册完成，共 {0} 个", enumTypes.Count());
        }

        // ══════════════════════════════════════════════════════════
        //  卡牌脚本加载
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 检查指定卡牌是否有 Lua 脚本文件。
        /// </summary>
        public bool HasCardScript(string cardId)
        {
            var path = Path.Combine(_luaCardsDir, $"{cardId}.lua");
            return File.Exists(path);
        }

        /// <summary>
        /// 加载卡牌 Lua 脚本，每次调用返回独立的 card table（不缓存）。
        /// 脚本在全局环境中执行，可访问 Init.lua 中定义的所有全局函数。
        /// </summary>
        /// <param name="cardId">卡牌 Define ID（如 "card_form_quan_da"）</param>
        /// <returns>脚本 return 的 card table，加载失败返回 null</returns>
        public LuaTable LoadCardScript(CardDefine define)
        {
            if (_luaState == null)
            {
                LogMgr.Instance.Err("[LuaMgr] LoadCardScript: Lua State 未初始化");
                return null;
            }

            var filePath = Path.Combine(_luaCardsDir, $"{define.ID}.lua");
            if (!File.Exists(filePath))
            {
                LogMgr.Instance.Dbg("[LuaMgr] LoadCardScript: 找不到脚本文件 {0}.lua", define.ID);
                return null;
            }

            try
            {
                var results = _luaState.DoFile(filePath);

                if (results != null && results.Length > 0 && results[0] is LuaTable cardTable)
                {
                    return cardTable;
                }
                else
                {
                    LogMgr.Instance.Warn("[LuaMgr] LoadCardScript '{0}': 脚本未 return table", define.ID);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogMgr.Instance.Err("[LuaMgr] LoadCardScript '{0}' 失败: {1}", define.ID, ex.Message);
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  Keyword 注册表
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 扫描 Keywords/ 目录下所有 .lua 文件并执行。
        /// 每个脚本内部负责调用 LuaMgr:RegisterKeyword(name, table) 完成自注册。
        /// </summary>
        private void LoadKeywordScripts()
        {
            if (!Directory.Exists(_luaKeywordsDir))
            {
                LogMgr.Instance.Warn("[LuaMgr] Keywords 目录不存在: {0}，跳过 Keyword 加载", _luaKeywordsDir);
                return;
            }

            var files = Directory.GetFiles(_luaKeywordsDir, "*.lua");
            foreach (var filePath in files)
            {
                try
                {
                    _luaState.DoFile(filePath);
                }
                catch (Exception ex)
                {
                    LogMgr.Instance.Err("[LuaMgr] Keyword 脚本执行失败: {0} -> {1}", Path.GetFileName(filePath), ex.Message);
                }
            }

            LogMgr.Instance.Dbg("[LuaMgr] Keyword 脚本扫描完成，已注册 {0} 个", _keywordRegistry.Count);
        }

        /// <summary>
        /// 注册一个 Keyword（供 Lua 脚本调用）。
        /// Lua 用法：LuaMgr:RegisterKeyword("Passive", table)
        /// </summary>
        public void RegisterKeyword(string name, LuaTable table)
        {
            if (string.IsNullOrEmpty(name))
            {
                LogMgr.Instance.Err("[LuaMgr] RegisterKeyword: name 不能为空");
                return;
            }
            if (table == null)
            {
                LogMgr.Instance.Err("[LuaMgr] RegisterKeyword: '{0}' 的 table 为 null", name);
                return;
            }

            _keywordRegistry[name] = table;
            LogMgr.Instance.Dbg("[LuaMgr] Keyword 注册成功: {0}", name);
        }

        /// <summary>
        /// 查询 Keyword 注册表，返回对应的 LuaTable；未找到返回 null。
        /// </summary>
        public LuaTable GetKeyword(string name)
        {
            return _keywordRegistry.TryGetValue(name, out var table) ? table : null;
        }

        // ══════════════════════════════════════════════════════════
        //  战斗 Modifier 脚本加载
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 加载战斗 Modifier Lua 脚本，每次调用返回独立的 LuaTable（不缓存）。
        /// 脚本路径：Data/LuaScripts/CombatModifiers/{defineId}.lua
        /// </summary>
        /// <param name="defineId">Modifier Define ID（如 "Burn"）</param>
        /// <returns>脚本 return 的 table，文件不存在或加载失败返回 null</returns>
        public LuaTable LoadModifierScript(string defineId)
        {
            if (_luaState == null)
            {
                LogMgr.Instance.Err("[LuaMgr] LoadModifierScript: Lua State 未初始化");
                return null;
            }

            var filePath = Path.Combine(_luaCombatModifiersDir, $"{defineId}.lua");
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                var results = _luaState.DoFile(filePath);

                if (results != null && results.Length > 0 && results[0] is LuaTable modTable)
                {
                    return modTable;
                }
                else
                {
                    LogMgr.Instance.Warn("[LuaMgr] LoadModifierScript '{0}': 脚本未 return table", defineId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogMgr.Instance.Err("[LuaMgr] LoadModifierScript '{0}' 失败: {1}", defineId, ex.Message);
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  LuaHooks 预扫描
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 扫描 LuaTable 中所有函数引用，返回 hookName → LuaFunction 字典。
        /// 供 ILuaBindable 实现者在加载 env 后调用，将结果赋给 LuaHooks。
        /// env 为 null 时返回空字典。
        /// </summary>
        public static Dictionary<string, LuaFunction> ScanLuaHooks(LuaTable env)
        {
            var hooks = new Dictionary<string, LuaFunction>();
            if (env == null) return hooks;
            foreach (var key in env.Keys)
            {
                if (key is string s && env[s] is LuaFunction func)
                    hooks[s] = func;
            }
            return hooks;
        }

    }
}