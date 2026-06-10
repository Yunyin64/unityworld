
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

--- 防御拼点（统一入口）
--- @param ctx APIContext
--- @param defendType string 防御类型（Shield/Block/Dodge）
--- @param value number 防御值
Defend = function(ctx, defendType, value)
    ctx:Set("DefendType", defendType)
    ctx:Set("DefendValue", tonumber(value))
    API:Execute("Defend", ctx)
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
--- @param domain string 选卡域（"All","Random","AboveOne","AboveAll","BelowOne","BelowAll","Adjacent","Self","Other","TargetAll","TargetRandom"）
--- @param reduceTick number 减少的tick数
Charge = function(ctx, domain, reduceTick)
    ctx:Set("Domain", domain)
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
--- @param domain string 选卡域
--- @param freezeTime number 冻结tick数
Freeze = function(ctx, domain, freezeTime)
    ctx:Set("Domain", domain)
    ctx:Set("FreezeTime", tonumber(freezeTime))
    API:Execute("Freeze", ctx)
end

--- 减速目标卡牌
--- @param ctx APIContext
--- @param domain string 选卡域
--- @param stack number 减速层数
Slow = function(ctx, domain, stack)
    ctx:Set("Domain", domain)
    ctx:Set("Stack", tonumber(stack))
    API:Execute("Slow", ctx)
end

--- 加速目标卡牌
--- @param ctx APIContext
--- @param domain string 选卡域
--- @param stack number 加速层数
Haste = function(ctx, domain, stack)
    ctx:Set("Domain", domain)
    ctx:Set("Stack", tonumber(stack))
    API:Execute("Haste", ctx)
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

--- 给施法者添加永久属性修正（轻量级，不走Define）
--- @param ctx APIContext
--- @param statId string 属性ID（如 "HpMax", "Atk", "Def"）
--- @param value number 修正值
--- @param modifierType string 修正类型（可选，默认"Flat"，可选"Percent"/"Override"/"ClampMax"/"ClampMin"）
--- @param sourceId string 来源标识（可选，用于后续移除）
AddStatBuff = function(ctx, statId, value, modifierType, sourceId)
    ctx:Set("StatId", statId)
    ctx:Set("Value", tonumber(value))
    if modifierType then
        ctx:Set("ModifierType", modifierType)
    end
    if sourceId then
        ctx:Set("SourceId", sourceId)
    end
    API:Execute("AddStatBuff", ctx)
end


-- ── 卡组操作类包装函数 ──────────────────────────────────────

--- 移除目标随机一张伤势卡
--- @param ctx APIContext
--- @param target CombatNpc 目标NPC
--- @param size number 伤势卡体量
--- @param exact boolean 是否精确匹配Size（可选，默认true）
RemoveRandomWound = function(ctx, target, size, exact)
    ctx:Set("Target", target)
    ctx:Set("Size", tonumber(size))
    if exact ~= nil then
        ctx:Set("Exact", exact)
    end
    API:Execute("RemoveRandomWound", ctx)
end

--- 位移目标卡牌到指定位置
--- @param ctx APIContext
--- @param domain string 选卡域
--- @param position string 位置（"First"/"Last"/"Random"）
Displace = function(ctx, domain, position)
    ctx:Set("Domain", domain)
    ctx:Set("Position", position)
    API:Execute("Displace", ctx)
end

--- 灵元转化回蓝条MP
--- @param ctx APIContext
--- @param element string 元素类型（"None"为随机凑满）
--- @param maxAmount number 最大转化数量
Convert = function(ctx, element, maxAmount)
    ctx:Set("Element", element)
    ctx:Set("MaxAmount", tonumber(maxAmount))
    API:Execute("Convert", ctx)
end

--- 减少自身指定元素的灵元
--- @param ctx APIContext
--- @param element string 元素类型（"None"为随机消耗）
--- @param amount number 减少数量
ReduceMana = function(ctx, element, amount)
    ctx:Set("Element", element)
    ctx:Set("Amount", tonumber(amount))
    API:Execute("ReduceMana", ctx)
end

--- 给目标卡牌添加永久属性修正
--- @param ctx APIContext
--- @param domain string 选卡域
--- @param statId string 属性ID（如 "ManaAdj_Jin", "CDTickAdj"）
--- @param value number 修正值
AddCardStatBuff = function(ctx, domain, statId, value)
    ctx:Set("Domain", domain)
    ctx:Set("StatId", statId)
    ctx:Set("Value", tonumber(value))
    API:Execute("AddCardStatBuff", ctx)
end
