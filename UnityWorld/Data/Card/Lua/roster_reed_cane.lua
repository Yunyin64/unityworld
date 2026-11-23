local card = setmetatable({}, { __index = CardBase })

function card:OnCardDisplaced(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round then return end
    self.rewardedRound = round
    RecoverMP(ctx, "Self", 1)
end

return card
