local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Shui", "SheJi", 4)
end

function card:Apply(ctx)
    Convert(ctx, "Self", "None", 2)
end

return card
