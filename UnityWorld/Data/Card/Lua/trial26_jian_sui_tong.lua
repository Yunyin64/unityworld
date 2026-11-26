-- 炉钳刺。发起 11 点火刺击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Huo", "Ci", 11)
end

return card
