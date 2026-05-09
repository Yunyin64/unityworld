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

-- ── 效果类包装函数 ──────────────────────────────────────────

--- 充能目标卡牌（减少CD）
--- @param ctx APIContext
--- @param cards table<CombatCard> 目标卡牌列表
--- @param reduceTick number 减少的tick数
Charge = function(ctx, cards, reduceTick)
    ctx:Set("TargetCard", cards)
    ctx:Set("ReduceTick", tonumber(reduceTick))
    API:Execute("Charge", ctx)
end

--- 破甲：消除对方护盾值
--- @param ctx APIContext
--- @param value number 破甲值
ArmorBreak = function(ctx, value)
    ctx:Set("BreakValue", tonumber(value))
    API:Execute("ArmorBreak", ctx)
end

--- 冻结目标卡牌
--- @param ctx APIContext
--- @param card CombatCard 目标卡牌
--- @param freezeTime number 冻结秒数
Freeze = function(ctx, card, freezeTime)
    ctx:Set("TargetCard", card)
    ctx:Set("FreezeTime", tonumber(freezeTime))
    API:Execute("Freeze", ctx)
end

--- 减速目标卡牌
--- @param ctx APIContext
Slow = function(ctx)
    API:Execute("Slow", ctx)
end

--- 立刻将MP转化为灵元
--- @param ctx APIContext
--- @param amount number 转化数量
Draw = function(ctx, amount)
    ctx:Set("Amount", tonumber(amount))
    API:Execute("Draw", ctx)
end

--- 给目标NPC添加Buff
--- @param ctx APIContext
--- @param target CombatNpc 目标NPC
--- @param buffId string Buff ID
--- @param stacks number 层数
--- @param duration number 持续时间（可选，-1为永久）
AddNpcBuff = function(ctx, target, buffId, stacks, duration)
    ctx:Set("Target", target)
    ctx:Set("BuffId", buffId)
    ctx:Set("Stacks", tonumber(stacks))
    if duration then
        ctx:Set("Duration", tonumber(duration))
    end
    API:Execute("AddNpcBuff", ctx)
end

-- ── 条件/查询类包装函数 ──────────────────────────────────────

--- 获得目标所有卡牌
--- @param ctx APIContext
--- @param target CombatNpc 目标NPC
--- @return boolean, table|nil  成功标志, 卡牌列表
AllCard = function(ctx, target)
    ctx:Set("Target", target)
    API:Execute("AllCard", ctx)
    return ctx:GetObject("Ret"), ctx:GetObject("Result")
end

--- 获得目标在CD中的一张随机卡牌
--- @param ctx APIContext
--- @param target CombatNpc 目标NPC
--- @return boolean, CombatCard|nil  成功标志, 卡牌
RandomCardInCD = function(ctx, target)
    ctx:Set("Target", target)
    API:Execute("RandomCardInCD", ctx)
    return ctx:GetObject("Ret"), ctx:GetObject("Result")
end

--- 获得目标相邻卡牌
--- @param ctx APIContext
--- @param target CombatCard 目标卡牌
--- @param direction string 方向（"Above"/"Below"）
--- @return boolean, table|nil  成功标志, 卡牌列表
AdjacentCards = function(ctx, target, direction)
    ctx:Set("Target", target)
    ctx:Set("Direction", direction)
    API:Execute("AdjacentCards", ctx)
    return ctx:GetObject("Ret"), ctx:GetObject("Result")
end