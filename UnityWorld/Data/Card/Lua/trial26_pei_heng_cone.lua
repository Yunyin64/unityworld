-- 贯潮锥。发起 11 点金刺击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "Ci", 11)
end

return card
