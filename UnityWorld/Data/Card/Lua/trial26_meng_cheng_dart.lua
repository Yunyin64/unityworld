-- 穿潮矢。发起 9 点金射击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "SheJi", 9)
end

return card
