local card = setmetatable({}, { __index = CardBase })

function card:OnPassiveActivated(ctx)
    self.lastMP = self.m_Owner:GetMp()
end

function card:OnMPChanged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local current = self.m_Owner:GetMp()
    local round = self.m_Owner.Backpack.Round
    local gained = current > (self.lastMP or current)
    self.lastMP = current
    if not gained or self.rewardedRound == round then return end
    self.rewardedRound = round
    Charge(ctx, "Self", 4)
end

return card
