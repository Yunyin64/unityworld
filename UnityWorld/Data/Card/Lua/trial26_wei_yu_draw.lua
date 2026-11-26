-- 重弦矢。发起 10 点金射击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "SheJi", 10)
end

return card
