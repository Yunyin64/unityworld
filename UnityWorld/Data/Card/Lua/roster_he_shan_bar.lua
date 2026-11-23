local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    local cards = self.m_Owner:GetField()
    for i = 0, cards.Count - 1 do
        if cards[i].Id == self.m_Self.ParentCardId then
            if CheckAmmoExhausted(ctx, cards[i]) then defend = defend + 1 end
            Defend(ctx, "Block", defend)
            return
        end
    end
    Defend(ctx, "Block", defend + 1)
end

return card
