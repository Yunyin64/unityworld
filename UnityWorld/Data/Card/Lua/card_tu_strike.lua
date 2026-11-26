-- 土玄打。发起3点土系打击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Tu", "Da", 3)
end

return card
