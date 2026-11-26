-- 眩晕。停止敌方背包的自然推进半秒。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    Stun(ctx, "Target", 5)
end

return card
