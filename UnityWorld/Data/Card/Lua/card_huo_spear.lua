-- 火枪。发起8点火系刺击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Huo", "Ci", 8)
end

return card
