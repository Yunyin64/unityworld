local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, atk, def, _, _, element = self:GetEquip()
    Defend(ctx, "Block", def)
end

return card
