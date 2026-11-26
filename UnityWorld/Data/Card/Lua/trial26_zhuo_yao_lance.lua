-- 并薪枪。发起 12 点火刺击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Huo", "Ci", 12)
end

return card
