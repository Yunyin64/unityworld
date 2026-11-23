local card = setmetatable({}, { __index = CardBase })

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.used = false
end

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not WasCardUsedOnSide(ctx, self.m_Self, "Up", "FaShu") then return end
    self.used = true
    -- 发动快照属于上邻；效果以本法宝的位置为准。
    local relative = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    ThawCard(relative, "GridDown")
    if HasDirectionalCard(relative, "Down", "ZhaoShi") then Charge(relative, "Self", 3) end
end

return card
