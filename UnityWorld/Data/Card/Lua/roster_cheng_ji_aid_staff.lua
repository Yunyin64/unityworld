local card = setmetatable({}, { __index = CardBase })

function card:OnHeal(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.shieldRound == round or self.m_Owner.ShieldValue >= 2 then return end
    self.shieldRound = round
    GainShield(ctx, "Self", 1)
end

return card
