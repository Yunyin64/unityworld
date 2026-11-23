local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    if self.m_Owner:GetSp() <= 9 then attack = attack + 1 end
    Attack(ctx, "Huo", "Da", attack)
end

return card
