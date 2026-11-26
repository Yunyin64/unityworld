-- 凉炉击。发起 10 点水打击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Shui", "Da", 10)
end

return card
