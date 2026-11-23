local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    if 2 * self.m_Owner:GetHp() <= self.m_Owner:GetCombatHpMax() then attack = attack + 1 end
    Attack(ctx, "Huo", "SheJi", attack)
end

return card
