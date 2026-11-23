local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round or ctx:GetStringValue("WinnerTypeName", "") ~= "Dodge" then return end
    if not CheckContestOutcome(ctx, self.m_Owner, "Win") or self.m_Owner.ShieldValue >= 2 then return end
    self.rewardedRound = round
    GainShield(ctx, "Self", 1)
end

return card
