local card = setmetatable({}, { __index = CardBase })

function card:OnManaCharged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not CheckManaCharged(ctx, "Any", "FaBao", true) then return end
    self.used = true
    GainShield(ctx, "Self", 2)
end

return card
