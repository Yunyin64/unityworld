local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    if self.m_Owner.ShieldValue >= 2 then defend = defend + 1 end
    Defend(ctx, "Block", defend)
end

return card
