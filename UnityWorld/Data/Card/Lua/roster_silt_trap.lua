local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Tu", "Ci", 6)
end

return card
