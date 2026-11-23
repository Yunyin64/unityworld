local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "Ci", 4)
end

return card
