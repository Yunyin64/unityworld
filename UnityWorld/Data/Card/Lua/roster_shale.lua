local card = setmetatable({}, { __index = CardBase })

function card:OnWoundGained(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not CheckWoundEvent(ctx, self.m_Owner, "Ally", 1) then return end
    self.used = true
    GainShield(ctx, "Self", 2)
end

return card
