-- 薄垒斩。发起 9 点土斩击拼点。效果结算时获得 3 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Tu", "Zhan", 9)
end

function card:Apply(ctx)
    GainShield(ctx, "Self", 3)
end

return card
