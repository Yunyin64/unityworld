local card = setmetatable({}, { __index = CardBase })

function card:OnWoundHealed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.healedRound = self.m_Owner.Backpack.Round
end

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    if self.healedRound == self.m_Owner.Backpack.Round then attack = attack + 1 end
    Attack(ctx, "Shui", "Da", attack)
end

return card
