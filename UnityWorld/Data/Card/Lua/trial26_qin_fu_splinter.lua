-- 叩刃刺。发起 6 点金刺击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "Ci", 6)
end

return card
