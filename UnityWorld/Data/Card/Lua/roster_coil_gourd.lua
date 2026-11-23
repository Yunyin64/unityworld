local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.cleanedRound == round or not HasKeyword(ctx, ctx.SourceCard, "FaShu") or not CheckManaUsed(ctx, "Kinds", 2) then return end
    self.cleanedRound = round
    RemoveElementBuff(ctx, "Self", "None", true, 1)
    RecoverMP(ctx, "Self", 1)
end

return card
