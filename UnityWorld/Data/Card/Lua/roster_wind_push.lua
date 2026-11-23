local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    SlideCard(ctx, "TargetRandom", "Right")
end

return card
