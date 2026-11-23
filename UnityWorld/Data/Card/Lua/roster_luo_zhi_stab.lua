local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if CheckManaUsed(ctx, "Kinds", 2) then self.mixedRound = self.m_Owner.Backpack.Round end
end

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    if self.mixedRound == self.m_Owner.Backpack.Round then attack = attack + 1 end
    Attack(ctx, "Mu", "Ci", attack)
end

return card
