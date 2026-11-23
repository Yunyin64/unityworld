local card = setmetatable({}, { __index = CardBase })

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx:GetObject("Round") == 1 then AddArtifactPower(ctx, "Self", "Huo", 1, 1) end
end

function card:OnFlexible(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not HasKeyword(ctx, ctx.SourceCard, "Consume") then return end
    self.used = true
    RecoverMP(ctx, "Self", 1)
end

return card
