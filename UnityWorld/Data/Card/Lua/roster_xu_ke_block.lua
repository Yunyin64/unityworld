local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    local field = self.m_Owner:GetField()
    local supplies = 0
    for i = 0, field.Count - 1 do
        local card = field[i]
        if HasKeyword(ctx, card, "Consume") and card.ConsumeStack > 0 then supplies = supplies + 1 end
    end
    if supplies == 1 then defend = defend + 1 end
    Defend(ctx, "Block", defend)
end

return card
