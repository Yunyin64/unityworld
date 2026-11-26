-- 春枝刺。发起 10 点木刺击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Mu", "Ci", 10)
end

return card
