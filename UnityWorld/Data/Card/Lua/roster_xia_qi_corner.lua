local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack, _, _, _, element = self:GetEquip()
    if HasDirectionalCard(ctx, "Down", "FaBao") then attack = attack + 1 end
    Attack(ctx, element, "Zhan", attack)
end

return card
