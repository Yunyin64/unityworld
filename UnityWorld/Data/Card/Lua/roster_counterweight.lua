local card = setmetatable({}, { __index = CardBase })

function card:OnDisplaceOtherCard(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used then return end
    self.used = true
    if self.m_Owner.ShieldValue < 3 then GainShield(ctx, "Self", 2) end
end

return card
