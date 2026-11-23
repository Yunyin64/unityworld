local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, atk, def, _, _, element = self:GetEquip()
    Attack(ctx, element, "Da", atk)
end

return card
