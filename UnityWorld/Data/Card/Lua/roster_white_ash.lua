local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArmor(ctx, "Self", 2)
end

return card
