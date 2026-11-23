local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if HasKeyword(ctx, ctx.SourceCard, "FaShu") and CheckManaUsed(ctx, "Huo", 2) then
        self.furnaceRound = self.m_Owner.Backpack.Round
    end
end

function card:OnDamage(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.furnaceRound ~= round or self.rewardedRound == round then return end
    self.rewardedRound = round
    RecoverMP(ctx, "Self", 2)
end

return card
