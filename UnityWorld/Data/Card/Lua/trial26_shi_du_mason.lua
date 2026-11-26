-- 落墙锤。发起 12 点土打击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Tu", "Da", 12)
end

return card
