local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.boostRound == round or not HasKeyword(ctx, ctx.SourceCard, "FaShu") or not CheckManaUsed(ctx, "Jin", 1) then return end
    self.boostRound = round
    local relative = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    ChangeEquipmentStat(relative, "Self", "Attack", 1, 1)
end

return card
