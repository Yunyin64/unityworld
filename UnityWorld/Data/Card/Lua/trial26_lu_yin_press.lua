-- 横金诀。发起 11 点金斩击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "Zhan", 11)
end

return card
