-- 晚锋斩。发起 10 点金斩击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "Zhan", 10)
end

return card
