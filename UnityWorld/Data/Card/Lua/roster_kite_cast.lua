local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    SlideCard(ctx, "GridDown", "Right")
end

return card
