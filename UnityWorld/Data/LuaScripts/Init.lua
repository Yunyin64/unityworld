-- ══════════════════════════════════════════════════════════════
-- Init.lua
-- 全局初始化：定义 CardBase 元表和 API 包装函数
-- 由 LuaMgr.Init() 加载，卡牌脚本可直接使用这里定义的全局变量
-- ══════════════════════════════════════════════════════════════

-- ── API 由 C# 侧注入（LuaMgr.LoadInitScript 中 _luaState["API"] = APIMgr.Instance）
-- 无需 import，直接使用全局变量 API

-- ── CardBase 元表 ────────────────────────────────────────────
CardBase = {}

-- ── 拼点类包装函数 ──────────────────────────────────────────

--- 攻击拼点：造成伤害
--- @param ctx APIContext C#传入的上下文
--- @param element string 元素类型（"Wu","Huo","Shui","Mu","Jin","Tu","None"）
--- @param physType string 物理类型（"Zhan","Ci","Da","SheJi"）
--- @param value number 攻击值
Attack = function(ctx, element, physType, value)
    ctx:Set("Element", element)
    ctx:Set("PhysicalType", physType)
    ctx:Set("AttackValue", tonumber(value))
    API:Execute("Attack", ctx)
end

--- 盾牌防御
--- @param ctx APIContext
--- @param value number 护盾值
Shield = function(ctx, value)
    ctx:Set("ShieldValue", tonumber(value))
    API:Execute("Shield", ctx)
end

--- 格挡防御
--- @param ctx APIContext
--- @param value number 格挡值
Block = function(ctx, value)
    ctx:Set("BlockValue", tonumber(value))
    API:Execute("Block", ctx)
end

--- 恢复HP
--- @param ctx APIContext
--- @param value number 治疗值
Heal = function(ctx, value)
    ctx:Set("HealValue", tonumber(value))
    API:Execute("Heal", ctx)
end

--- 自伤
--- @param ctx APIContext
--- @param value number 伤害值
SelfDamage = function(ctx, value)
    ctx:Set("DamageValue", tonumber(value))
    API:Execute("SelfDamage", ctx)
end