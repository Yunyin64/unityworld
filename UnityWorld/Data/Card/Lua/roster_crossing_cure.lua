local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Shui", 200, 0)
    ChangeAffinity(ctx, "Self", "Jin", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.used = false
end

function card:OnWoundHealed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not CheckWoundEvent(ctx, self.m_Owner, "Ally", 1) then return end
    self.used = true
    if self.m_Owner.ShieldValue < 3 then GainShield(ctx, "Self", 2) end
end

return card
