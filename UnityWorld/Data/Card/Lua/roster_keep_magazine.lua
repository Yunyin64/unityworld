local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Jin", 200, 0)
    ChangeAffinity(ctx, "Self", "Huo", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.used = false
    if ctx:GetObject("Round") == 1 then AddArtifactPower(ctx, "Self", "Jin", 1, 1) end
end

function card:OnFlexible(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used then return end
    self.used = true
    Charge(ctx, "Self", 5)
end

return card
