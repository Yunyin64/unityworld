local card = setmetatable({}, { __index = CardBase })

function card:OnAmmoExhausted(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not CheckAmmoExhausted(ctx, ctx.SourceCard) then return end
    self.used = true
    RecoverMP(ctx, "Self", 1)
    if self.m_Owner.ShieldValue == 0 then GainShield(ctx, "Self", 1) end
end

return card
