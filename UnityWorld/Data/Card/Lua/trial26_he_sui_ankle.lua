-- 折踝诀。发起 10 点闪避拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Defend(ctx, "Dodge", 10)
end

return card
