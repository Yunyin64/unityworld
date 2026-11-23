local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack, _, _, _, element = self:GetEquip()
    if 2 * self.m_Owner:GetHp() <= self.m_Owner:GetCombatHpMax() then attack = attack + 1 end
    Attack(ctx, element, "Da", attack)
end

return card
