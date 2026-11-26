-- 撞垛击。发起 11 点土打击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Tu", "Da", 11)
end

return card
