local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    if self.m_Owner.ShieldValue >= 2 then attack = attack + 1 end
    Attack(ctx, "Huo", "SheJi", attack)
end

return card
