local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "Zhan", 4)
end

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 1)
end

return card
