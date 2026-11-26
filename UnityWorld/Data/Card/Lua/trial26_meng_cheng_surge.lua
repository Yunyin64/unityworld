-- 过堤射。发起 12 点水射击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Shui", "SheJi", 12)
end

return card
