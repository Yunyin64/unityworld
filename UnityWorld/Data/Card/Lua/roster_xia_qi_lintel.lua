local card = setmetatable({}, { __index = CardBase })

function card:OnStraight(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round or self.m_Owner.ShieldValue ~= 0 then return end
    self.rewardedRound = round
    GainShield(ctx, "Self", 1)
end

return card
