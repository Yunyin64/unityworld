-- 短焰射。发起 8 点火射击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Huo", "SheJi", 8)
end

return card
