local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 3)
end

return card
