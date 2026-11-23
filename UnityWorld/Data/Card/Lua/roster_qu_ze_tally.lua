local card = setmetatable({}, { __index = CardBase })

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.used = false
end

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not CheckManaUsed(ctx, "Shui", 2) then return end
    self.used = true
    if self.m_Owner.ShieldValue == 0 then GainShield(ctx, "Self", 1) end
end

return card
