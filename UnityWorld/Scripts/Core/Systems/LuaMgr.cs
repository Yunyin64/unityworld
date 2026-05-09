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

        /// <summary>Lua 初始化脚本路径</summary>
        private readonly string _luaInitPath;

        // ── Hook 函数名 → EventId 映射表 ────────────────────────

        /// <summary>
        /// 约定的 Hook 函数名到 EventMgr 事件 ID 的映射。
        /// OnUse 不在此表中（由框架直接调用，不注册事件）。
        /// </summary>
        public static readonly Dictionary<string, string> HookToEventId = new();

        /// <summary>Keyword 注册表：keyword 名称 → 对应 Lua 脚本返回的 table</summary>
        private Dictionary<string, LuaTable> _keywordRegistry = new();

        // ── 构造 ────────────────────────────────────────────────

        /// <summary>
        /// 构造 LuaMgr。
        /// </summary>
        /// <param name="luaCardsDir">Lua 卡牌脚本目录（默认 Data/LuaCards）</param>
        public LuaMgr(string? luaCardsDir = null)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _luaCardsDir = luaCardsDir ?? Path.Combine(baseDir, "Data", "LuaCards");
            _luaInitPath = Path.Combine(baseDir, "Data", "LuaScripts", "Init.lua");
            Instance = this;
        }

        // ── 生命周期 ────────────────────────────────────────────

        /// <summary>
        /// 初始化：创建 Lua State，加载 Init.lua。
        /// </summary>
        public void Init()
        {
            _luaState = new Lua();
            _luaState.LoadCLRPackage();

            // 加载 Init.lua（定义 CardBase、Attack 等全局函数）
            LoadInitScript();

            // 加载 Keyword 注册表
            LoadKeywords();

            LogMgr.Dbg("[LuaMgr] 初始化完成，Lua State 已创建");
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
            LogMgr.Dbg("=== {0} ===  {1}", Name, Desc);
            LogMgr.Dbg("  Lua State: {0}", _luaState != null ? "Active" : "Null");
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
                LogMgr.Err("[LuaMgr] Init.lua 不存在: {0}", _luaInitPath);
                return;
            }

            try
            {
                // 注入 APIMgr 实例到 Lua 全局空间，供 Init.lua 使用
                if (APIMgr.Instance != null)
                    _luaState["API"] = APIMgr.Instance;

                _luaState.DoFile(_luaInitPath);
                LogMgr.Dbg("[LuaMgr] Init.lua 加载成功");
            }
            catch (Exception ex)
            {
                LogMgr.Err("[LuaMgr] Init.lua 加载失败: {0}", ex.Message);
            }
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
        public LuaTable LoadCardScript(string cardId)
        {
            if (_luaState == null)
            {
                LogMgr.Err("[LuaMgr] LoadCardScript: Lua State 未初始化");
                return null;
            }

            var filePath = Path.Combine(_luaCardsDir, $"{cardId}.lua");
            if (!File.Exists(filePath))
            {
                LogMgr.Dbg("[LuaMgr] LoadCardScript: 找不到脚本文件 {0}.lua", cardId);
                return null;
            }

            try
            {
                var results = _luaState.DoFile(filePath);

                if (results != null && results.Length > 0 && results[0] is LuaTable cardTable)
                {
                    LogMgr.Dbg("[LuaMgr] 加载卡牌脚本成功: {0}", cardId);
                    return cardTable;
                }
                else
                {
                    LogMgr.Warn("[LuaMgr] LoadCardScript '{0}': 脚本未 return table", cardId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogMgr.Err("[LuaMgr] LoadCardScript '{0}' 失败: {1}", cardId, ex.Message);
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  Keyword 注册表
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 加载 Keyword 索引文件并逐个加载 keyword Lua 脚本，缓存到注册表。
        /// 索引文件路径：Data/LuaScripts/Keywords/Keyword.lua
        /// </summary>
        private void LoadKeywords()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var keywordsDir = Path.Combine(baseDir, "Data", "LuaScripts", "Keywords");
            var indexPath = Path.Combine(keywordsDir, "Keyword.lua");

            if (!File.Exists(indexPath))
            {
                LogMgr.Warn("[LuaMgr] Keywords/Keyword.lua 不存在: {0}，跳过 Keyword 加载", indexPath);
                return;
            }

            try
            {
                var results = _luaState.DoFile(indexPath);
                if (results == null || results.Length == 0 || results[0] is not LuaTable indexTable)
                {
                    LogMgr.Warn("[LuaMgr] Keywords/Keyword.lua 未返回 table");
                    return;
                }

                foreach (var key in indexTable.Keys)
                {
                    var kwName = key.ToString();
                    var kwRelPath = indexTable[key]?.ToString();
                    if (string.IsNullOrEmpty(kwRelPath)) continue;

                    var kwFilePath = Path.Combine(keywordsDir, $"{kwRelPath}.lua");
                    if (!File.Exists(kwFilePath))
                    {
                        LogMgr.Err("[LuaMgr] Keyword 脚本不存在: {0} -> {1}", kwName, kwFilePath);
                        continue;
                    }

                    try
                    {
                        var kwResults = _luaState.DoFile(kwFilePath);
                        if (kwResults != null && kwResults.Length > 0 && kwResults[0] is LuaTable kwTable)
                        {
                            _keywordRegistry[kwName] = kwTable;
                            LogMgr.Dbg("[LuaMgr] Keyword 加载成功: {0}", kwName);
                        }
                        else
                        {
                            LogMgr.Warn("[LuaMgr] Keyword '{0}' 脚本未返回 table", kwName);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMgr.Err("[LuaMgr] Keyword '{0}' 加载失败: {1}", kwName, ex.Message);
                    }
                }

                LogMgr.Dbg("[LuaMgr] Keyword 注册表加载完成，共 {0} 个", _keywordRegistry.Count);
            }
            catch (Exception ex)
            {
                LogMgr.Err("[LuaMgr] Keywords/Keyword.lua 加载失败: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 查询 Keyword 注册表，返回对应的 LuaTable；未找到返回 null。
        /// </summary>
        public LuaTable GetKeyword(string name)
        {
            return _keywordRegistry.TryGetValue(name, out var table) ? table : null;
        }

    }
}