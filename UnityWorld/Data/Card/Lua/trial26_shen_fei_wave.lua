-- 雪片斩。发起 9 点水斩击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Shui", "Zhan", 9)
end

return card
