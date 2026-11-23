local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    Attack(ctx, "Jin", "SheJi", attack)
end

return card
