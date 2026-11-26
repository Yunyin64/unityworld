-- 叩垒矢。发起 6 点土射击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Tu", "SheJi", 6)
end

return card
