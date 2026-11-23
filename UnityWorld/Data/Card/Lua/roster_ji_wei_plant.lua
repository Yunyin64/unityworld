local card = setmetatable({}, { __index = CardBase })

function card:OnDisplaceOtherCard(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.pushedRound = self.m_Owner.Backpack.Round
end

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    if self.pushedRound == self.m_Owner.Backpack.Round then attack = attack + 1 end
    Attack(ctx, "Mu", "Zhan", attack)
end

return card
