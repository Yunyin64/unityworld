local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    if self.m_Owner.ShieldValue == 0 then defend = defend + 1 end
    Defend(ctx, "Shield", defend)
end

return card
