-- 余炉刺。发起 10 点火刺击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Huo", "Ci", 10)
end

return card
