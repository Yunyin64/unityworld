local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Mu", "Zhan", 5)
end

return card
