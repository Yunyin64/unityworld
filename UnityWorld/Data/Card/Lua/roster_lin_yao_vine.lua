local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Mu", "Zhan", 4)
end

function card:Apply(ctx)
    StripCardMana(ctx, "TargetRandom", "Mu", 1)
end

return card
