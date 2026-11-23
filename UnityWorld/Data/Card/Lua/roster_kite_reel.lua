local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Shui", "Zhan", 4)
end

function card:Apply(ctx)
    SlideCard(ctx, "GridUp", "Left")
end

return card
