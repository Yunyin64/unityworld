local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Mu", "Da", 4)
end

function card:Apply(ctx)
    SlideCard(ctx, "TargetRandom", "Left")
end

return card
