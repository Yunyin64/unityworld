local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Shui", 200, 0)
    ChangeAffinity(ctx, "Self", "Mu", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.used = false
end

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or ctx:GetStringValue("WinnerTypeName", "") ~= "Shield" then return end
    if not CheckContestOutcome(ctx, self.m_Owner, "Win") then return end
    self.used = true
    if self.m_Owner.ShieldValue < 2 then
        local overflow = ctx:GetObject("Overflow")
        if overflow > 0 then GainShield(ctx, "Self", math.min(overflow, 2)) end
    end
end

return card
