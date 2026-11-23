local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Mu", 200, 0)
    ChangeAffinity(ctx, "Self", "Shui", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.used = false
end

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not WasCardUsedOnSide(ctx, self.m_Self, "Right", "FaShu") then return end
    self.used = true
    Haste(ctx, "Self", 7)
end

return card
