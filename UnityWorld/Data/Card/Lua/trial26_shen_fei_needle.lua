-- 霜窗刺。发起 10 点水刺击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Shui", "Ci", 10)
end

return card
