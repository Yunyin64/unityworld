local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    if self.m_Owner.ShieldValue >= 3 then defend = math.max(0, defend - 1) end
    Defend(ctx, "Shield", defend)
end

return card
