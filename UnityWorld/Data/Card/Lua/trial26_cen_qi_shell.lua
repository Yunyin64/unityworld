-- 胎贝衣。开场获得 2 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    GainShield(ctx, "Self", 2)
end

return card
