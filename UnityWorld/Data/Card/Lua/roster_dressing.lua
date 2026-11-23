local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    Heal(ctx, "Self", 2)
    RemoveRandomWound(ctx, "Self", 1, false)
end

return card
