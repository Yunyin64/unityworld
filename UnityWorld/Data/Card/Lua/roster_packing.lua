local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    RefillAmmo(ctx, "GridUp", 1)
end

return card
