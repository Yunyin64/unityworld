local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    Attack(ctx, "Jin", "SheJi", attack)
end

function card:Apply(ctx)
    local cards = self.m_Owner:GetField()
    for i = 0, cards.Count - 1 do
        if cards[i].Id == self.m_Self.ParentCardId then return end
    end
    ArmorBreak(ctx, "Target", 1)
end

return card
