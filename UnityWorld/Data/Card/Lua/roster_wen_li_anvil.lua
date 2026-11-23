local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.temperRound == round or not HasKeyword(ctx, ctx.SourceCard, "FaShu") or not CheckManaUsed(ctx, "Tu", 1) then return end
    self.temperRound = round
    local relative = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    ChangeEquipmentStat(relative, "Self", "Defend", 1, 1)
end

return card
