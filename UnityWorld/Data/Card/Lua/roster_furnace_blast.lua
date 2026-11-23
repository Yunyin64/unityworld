local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Huo", "SheJi", 8)
end

function card:Apply(ctx)
    SelfDamage(ctx, "Self", 2)
end

return card
