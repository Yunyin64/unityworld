-- [Deprecated] 已迁移至 APIDomainFunc，保留供参考。新代码请使用 Action 的 Domain 参数选卡。

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
