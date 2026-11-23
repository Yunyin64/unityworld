local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Mu", "Ci", 4)
end

function card:OnStraight(ctx)
    if ctx.SourceCard ~= self.m_Self then return end
    Charge(ctx, "Self", 4)
end

function card:OnContestOverflow(ctx)
    if ctx.SourceCard ~= self.m_Self or not CheckContestOutcome(ctx, self.m_Owner, "Win") then return end
    Charge(ctx, "Self", 4)
end

return card
