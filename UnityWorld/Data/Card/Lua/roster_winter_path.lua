local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Shui", 200, 0)
    ChangeAffinity(ctx, "Self", "Tu", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = ctx:GetObject("Round")
    if round % 6 == 1 then AddArtifactPower(ctx, "Self", "Shui", 1, 1) end
end

return card
