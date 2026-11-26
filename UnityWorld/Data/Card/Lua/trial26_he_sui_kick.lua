-- 断步踢。发起 13 点土打击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Tu", "Da", 13)
end

return card
