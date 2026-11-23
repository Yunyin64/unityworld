local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not CheckManaUsed(ctx, "Kinds", 2) then return end
    self.used = true
    GainShield(ctx, "Self", 2)
end

return card
