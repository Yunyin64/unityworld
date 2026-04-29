using System.Collections;
using System.Reflection;
using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Lua 运行时管理器：管理 NLua State 生命周期、C# API 暴露、
    /// 卡牌脚本加载与 Hook 函数发现。
    /// </summary>
    public class LuaMgr : IDomainMgrBase
    {
        public static LuaMgr Instance { get; private set; }
        public string Name => "LuaMgr";
        public string Desc => "Lua运行时管理器：NLua State 生命周期、C# API 暴露、脚本加载";

        // ── Lua State ────────────────────────────────────────────

        /// <summary>全局 Lua State（整个应用共享一个）</summary>
        private Lua _luaState;

        /// <summary>已加载的卡牌脚本环境：cardId → LuaTable（独立环境）</summary>
        private readonly Dictionary<string, LuaTable> _cardEnvironments = new();

        /// <summary>Lua 卡牌脚本目录路径</summary>
        private readonly string _luaCardsDir;

        // ── Hook 函数名 → EventId 映射表 ────────────────────────

        /// <summary>
        /// 约定的 Hook 函数名到 EventMgr 事件 ID 的映射。
        /// OnUse 不在此表中（由框架直接调用，不注册事件）。
        /// </summary>
        public static readonly Dictionary<string, string> HookToEventId = new();
        // ── 构造 ────────────────────────────────────────────────

        /// <summary>
        /// 构造 LuaMgr。
        /// </summary>
        /// <param name="luaCardsDir">Lua 卡牌脚本目录（默认 Data/LuaCards）</param>
        public LuaMgr(string? luaCardsDir = null)
        {
            _luaCardsDir = luaCardsDir ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "LuaCards");
            Instance = this;
        }

        // ── 生命周期 ────────────────────────────────────────────

        /// <summary>
        /// 初始化：创建 Lua State，注册 C# API 为全局函数。
        /// </summary>
        public void Init()
        {
            _luaState = new Lua();
            _luaState.LoadCLRPackage();

            // 注册 C# API 函数到 Lua 全局空间
            RegisterCSharpAPIs();

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
        /// 销毁：Dispose Lua State，清理所有缓存环境。
        /// </summary>
        public void End()
        {
            _cardEnvironments.Clear();
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
            LogMgr.Dbg("  已加载卡牌脚本: {0} 个", _cardEnvironments.Count);
        }

        // ══════════════════════════════════════════════════════════
        //  C# API 注册
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 调用底层 API。
        /// </summary>
        private void RegisterCSharpAPIs()
        {
            if (_luaState == null) return;

            int count = 0;
            foreach (var api in APIMgr.Instance.GetAll())
            {
                _luaState.RegisterFunction(api.FuncName, api.Method);
                count++;
            }

            LogMgr.Dbg("[LuaMgr] 已注册 {0} 个 C# API 函数到 Lua 全局空间", count);
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
        /// 加载卡牌 Lua 脚本到独立环境。
        /// 每张卡牌获得自己的环境表，继承全局 API 但局部变量互不干扰。
        /// </summary>
        /// <param name="cardId">卡牌 Define ID（如 "card_jin_whirlwind"）</param>
        /// <returns>脚本环境表，加载失败返回 null</returns>
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
                LogMgr.Warn("[LuaMgr] LoadCardScript: 找不到脚本文件 {0}", filePath);
                return null;
            }

            try
            {
                var scriptContent = File.ReadAllText(filePath);

                // 创建独立环境表：继承全局表（_G）但有自己的局部空间
                var env = CreateIsolatedEnvironment();

                // 将环境表存入全局变量，供 load 使用
                _luaState["__card_env"] = env;

                // 在独立环境中执行脚本
                _luaState.DoString($@"
                    local env = __card_env
                    __card_env = nil
                    local fn, err = load([=[{EscapeLuaString(scriptContent)}]=], '{cardId}', 't', env)
                    if fn then
                        fn()
                    else
                        error(err)
                    end
                ");

                _cardEnvironments[cardId] = env;

                LogMgr.Dbg("[LuaMgr] 加载卡牌脚本成功: {0}", cardId);
                return env;
            }
            catch (Exception ex)
            {
                LogMgr.Err("[LuaMgr] LoadCardScript '{0}' 失败: {1}", cardId, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 卸载指定卡牌的 Lua 环境（战斗结束时清理）。
        /// </summary>
        public void UnloadCardScript(string cardId)
        {
            _cardEnvironments.Remove(cardId);
        }

        /// <summary>
        /// 清理所有已加载的卡牌环境。
        /// </summary>
        public void UnloadAllCardScripts()
        {
            _cardEnvironments.Clear();
        }

        /// <summary>
        /// 获取已加载的卡牌环境。
        /// </summary>
        public LuaTable? GetCardEnvironment(string cardId)
        {
            return _cardEnvironments.TryGetValue(cardId, out var env) ? env : null;
        }

        // ══════════════════════════════════════════════════════════
        //  Hook 函数发现
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 扫描卡牌 Lua 环境中所有 OnXxx 函数名，返回发现的 Hook 列表。
        /// </summary>
        /// <param name="env">卡牌的 Lua 环境表</param>
        /// <returns>发现的 Hook 函数名列表（如 ["OnUse", "OnAttack"]）</returns>
        public List<string> DiscoverHooks(LuaTable env)
        {
            var hooks = new List<string>();
            if (_luaState == null || env == null) return hooks;

            // 检查 CombatCard 表是否存在
            var combatCardTable = env["CombatCard"] as LuaTable;
            if (combatCardTable == null)
            {
                // 如果没有 CombatCard 表，直接在环境中查找 OnXxx 函数
                foreach (var hookName in HookToEventId.Keys)
                {
                    if (env[hookName] is LuaFunction)
                        hooks.Add(hookName);
                }
                if (env["OnUse"] is LuaFunction)
                    hooks.Add("OnUse");
            }
            else
            {
                // 在 CombatCard 表中查找 OnXxx 函数
                if (combatCardTable["OnUse"] is LuaFunction)
                    hooks.Add("OnUse");

                foreach (var hookName in HookToEventId.Keys)
                {
                    if (combatCardTable[hookName] is LuaFunction)
                        hooks.Add(hookName);
                }
            }

            return hooks;
        }

        // ══════════════════════════════════════════════════════════
        //  Lua 函数调用
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 调用卡牌环境中的 Lua 函数（通过 CombatCard:hookName 方式）。
        /// </summary>
        /// <param name="cardId">卡牌 ID</param>
        /// <param name="hookName">Hook 函数名（如 "OnUse"、"OnAttack"）</param>
        /// <param name="ctxTable">Lua context table</param>
        public void CallCardHook(string cardId, string hookName, LuaTable ctxTable)
        {
            if (_luaState == null) return;

            var env = GetCardEnvironment(cardId);
            if (env == null)
            {
                LogMgr.Warn("[LuaMgr] CallCardHook: 未找到卡牌环境 {0}", cardId);
                return;
            }

            try
            {
                var combatCardTable = env["CombatCard"] as LuaTable;
                LuaFunction? func = null;

                if (combatCardTable != null)
                    func = combatCardTable[hookName] as LuaFunction;
                else
                    func = env[hookName] as LuaFunction;

                if (func == null)
                {
                    LogMgr.Warn("[LuaMgr] CallCardHook: {0} 中未找到函数 {1}", cardId, hookName);
                    return;
                }

                // 调用：self = combatCardTable（或 env），ctx = ctxTable
                if (combatCardTable != null)
                    func.Call(combatCardTable, ctxTable);
                else
                    func.Call(ctxTable);
            }
            catch (Exception ex)
            {
                LogMgr.Err("[LuaMgr] CallCardHook '{0}.{1}' 异常: {2}", cardId, hookName, ex.Message);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  Context 适配（C# → Lua table）
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 构建 Lua context table，包含 Caster/Target/SelfCardId 等战斗上下文信息。
        /// </summary>
        public LuaTable? CreateContextTable(
            Combat.CombatNpc? caster,
            Combat.CombatNpc? target,
            string selfCardId,
            object? eventArgs = null)
        {
            if (_luaState == null) return null;

            var table = _luaState.DoString("return {}")[0] as LuaTable;
            if (table == null) return null;

            // 基础字段
            if (caster != null) table["Caster"] = caster;
            if (target != null) table["Target"] = target;
            table["SelfCardId"] = selfCardId;

            // 事件特定数据
            if (eventArgs != null) table["EventArgs"] = eventArgs;

            return table;
        }

        // ══════════════════════════════════════════════════════════
        //  内部辅助
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 创建独立的 Lua 环境表，继承全局表的 API 但隔离局部变量。
        /// </summary>
        private LuaTable CreateIsolatedEnvironment()
        {
            var results = _luaState!.DoString(@"
                local env = {}
                setmetatable(env, {__index = _G})
                env.CombatCard = {}
                return env
            ");
            return (LuaTable)results[0];
        }

        /// <summary>
        /// 转义 Lua 长字符串中的特殊序列。
        /// </summary>
        private static string EscapeLuaString(string input)
        {
            // 长字符串 [=[ ... ]=] 只需要避免 ]=] 出现在内容中
            // 如果内容包含 ]=]，增加等号级别
            return input;
        }
    }
}