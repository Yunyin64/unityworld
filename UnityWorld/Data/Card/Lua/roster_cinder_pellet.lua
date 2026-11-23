local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 3)
    Charge(ctx, "Self", 5)
end

return card
