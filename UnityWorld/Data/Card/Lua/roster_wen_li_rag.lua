local card = setmetatable({}, { __index = CardBase })

function card:OnShieldExhausted(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not CheckShieldExhaustion(ctx, "Damage") then return end
    self.used = true
    AddArmor(ctx, "Self", 2)
end

return card
