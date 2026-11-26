-- 闭门斩。发起 12 点金斩击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "Zhan", 12)
end

return card
