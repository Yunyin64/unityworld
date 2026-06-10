-- ══════════════════════════════════════════════════════════════
-- Init.lua
-- 全局初始化：定义 CardBase 元表和 API 包装函数
-- 由 LuaMgr.Init() 加载，卡牌脚本可直接使用这里定义的全局变量
-- ══════════════════════════════════════════════════════════════

-- ── C# 侧已注入全局变量：LuaMgr, LogMgr, API ────────────────

-- ── 加载同级 Lua 模块 ──────────────────────────────────────
require("Action")
require("Aura")
require("Condition")
require("Contest")

-- ── CardBase 元表 ────────────────────────────────────────────
CardBase = {}


--- 全局日志快捷函数
Log = function(msg)
    LogMgr:Dbg("[Lua] {0}", tostring(msg))
end
