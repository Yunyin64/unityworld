local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    Heal(ctx, "Self", 3)
    RemoveRandomWound(ctx, "Self", 2, false)
end

return card
