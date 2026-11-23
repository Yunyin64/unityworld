local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Jin", 200, 0)
    ChangeAffinity(ctx, "Self", "Tu", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.wonForm = false
end

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if CheckContestOutcome(ctx, self.m_Owner, "Win") and HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then
        self.wonForm = true
    end
end

function card:OnRoundEnd(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not self.wonForm then RecoverMP(ctx, "Self", 2) end
end

return card
