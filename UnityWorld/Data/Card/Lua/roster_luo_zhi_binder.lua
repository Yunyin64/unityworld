local card = setmetatable({}, { __index = CardBase })

function card:OnWoundGained(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used then return end
    self.used = true
    if self.m_Owner.ShieldValue == 0 then GainShield(ctx, "Self", 1) end
end

return card
