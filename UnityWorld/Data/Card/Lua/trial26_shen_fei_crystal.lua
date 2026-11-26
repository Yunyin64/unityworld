-- 晶尾刺。发起 8 点水刺击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Shui", "Ci", 8)
end

return card
