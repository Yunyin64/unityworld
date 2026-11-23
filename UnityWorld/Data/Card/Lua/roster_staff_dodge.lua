local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, atk, def, _, _, element = self:GetEquip()
    Defend(ctx, "Dodge", def)
end

return card
