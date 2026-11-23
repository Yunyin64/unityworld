local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Huo", 200, 0)
    ChangeAffinity(ctx, "Self", "Mu", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx:GetObject("Round") == 1 then AddArtifactPower(ctx, "Self", "Huo", 1, 1) end
end

function card:OnRoundEnd(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    RecoverMP(ctx, "Self", 1)
end

return card
