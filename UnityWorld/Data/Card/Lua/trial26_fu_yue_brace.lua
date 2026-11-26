-- 手墙格。发起 6 点格挡拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Defend(ctx, "Block", 6)
end

return card
