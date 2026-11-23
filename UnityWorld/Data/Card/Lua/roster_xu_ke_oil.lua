local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Huo", "Zhan", 6)
end

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 2)
end

return card
