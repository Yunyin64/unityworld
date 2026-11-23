local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Jin", 200, 0)
    ChangeAffinity(ctx, "Self", "Shui", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.used = false
end

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not CheckContestOutcome(ctx, self.m_Owner, "Win") then return end
    self.used = true
    ArmorBreak(ctx, "Target", 1)
end

return card
