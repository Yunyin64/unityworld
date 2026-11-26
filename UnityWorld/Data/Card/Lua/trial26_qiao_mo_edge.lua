-- 分线刃。发起 9 点金斩击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "Zhan", 9)
end

return card
