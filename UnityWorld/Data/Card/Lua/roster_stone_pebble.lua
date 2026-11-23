local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Tu", "SheJi", 4)
end

return card
