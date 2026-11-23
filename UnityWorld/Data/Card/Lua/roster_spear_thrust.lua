local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, atk, def, _, _, element = self:GetEquip()
    Attack(ctx, element, "Ci", atk)
end

return card
