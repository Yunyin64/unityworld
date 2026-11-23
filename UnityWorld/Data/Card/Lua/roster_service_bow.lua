local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.boostRound == round or not WasCardUsedOnSide(ctx, self.m_Self, "Down", "Consume") then return end
    self.boostRound = round
    local relative = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    ChangeEquipmentStat(relative, "Self", "Attack", 2, 1)
end

return card
