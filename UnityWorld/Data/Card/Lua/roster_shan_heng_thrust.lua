local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack, _, _, _, element = self:GetEquip()
    Attack(ctx, element, "Ci", attack)
end

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 1)
end

return card
