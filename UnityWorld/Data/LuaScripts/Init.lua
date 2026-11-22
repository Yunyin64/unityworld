-- ══════════════════════════════════════════════════════════════
-- Init.lua
-- 全局初始化：定义 CardBase 元表和 API 包装函数
-- 由 LuaMgr.Init() 加载，卡牌脚本可直接使用这里定义的全局变量
-- ══════════════════════════════════════════════════════════════

-- ── C# 侧已注入全局变量：LuaMgr, LogMgr, API ────────────────

-- ── 加载同级 Lua 模块 ──────────────────────────────────────
local scopes = {"Global", "Npc", "Tile", "Plane", "Card", "CombatCard", "CombatNpc"}
local types  = {"Action", "Aura", "Condition", "Contest", "Scope"}

for _, scope in ipairs(scopes) do
    for _, apiType in ipairs(types) do
        local mod = scope .. "_" .. apiType
        local ok, err = pcall(require, mod)
    end
end

-- ── CardBase 元表 ────────────────────────────────────────────
CardBase = {}
BuffBase = {}

-- 统一读取卡自身装备或招式绑定的父装备。保留招式脚本的返回顺序。
function CardBase:GetEquip()
    local equip = self.m_Self:GetEquipData()
    local name = equip:GetStringValue("DisplayName", "")
    local attack = equip:GetValue("Attack", 0)
    local defend = equip:GetValue("Defend", 0)
    local speed = equip:GetValue("Speed", 0)
    local amount = equip:GetValue("Amount", 0)
    local element = equip:GetStringValue("Element", "Wu")
    return name, attack, defend, speed, amount, element
end

-- ── KeywordBase 元表 ─────────────────────────────────────────
KeywordBase = {}
function KeywordBase.CheckCondition(card)
    return false
end


-- 末格的主词检查与实际支付共用卡上的费用关键词。费用函数独立于战斗事件表。
local function visitScanCosts(card, operation)
    local names = card:GetKeywords()
    for i = 0, names.Count - 1 do
        local keyword = LuaMgr:GetKeyword(names[i])
        local cost = keyword and keyword.ScanCost
        if cost and cost[operation](card) == false then return false end
    end
    return true
end

function KeywordBase.CanPayScanCosts(card)
    return visitScanCosts(card, "CanPay")
end

function KeywordBase.PayScanCosts(card)
    visitScanCosts(card, "Pay")
end

--- 全局日志快捷函数
Log = function(msg)
    LogMgr:Dbg("[Lua] {0}", tostring(msg))
end
