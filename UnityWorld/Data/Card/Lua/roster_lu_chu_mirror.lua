local card = setmetatable({}, { __index = CardBase })

function card:OnShieldExhausted(ctx)
    if ctx.Caster == nil or ctx.Caster.Team == self.m_Owner.Team then return end
    if ctx:GetObject("SourceNpc") ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round then return end
    self.rewardedRound = round
    local source = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    Charge(source, "Self", 5)
end

return card
