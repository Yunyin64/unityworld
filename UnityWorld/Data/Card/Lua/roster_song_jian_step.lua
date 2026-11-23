local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    Defend(ctx, "Dodge", defend)
end

return card
