local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round or not HasKeyword(ctx, ctx.SourceCard, "FaShu") or not CheckManaUsed(ctx, "Jin", 1) then return end
    self.rewardedRound = round
    Convert(ctx, "Self", "Huo", 1)
    Convert(ctx, "Self", "Tu", 1)
end

return card
