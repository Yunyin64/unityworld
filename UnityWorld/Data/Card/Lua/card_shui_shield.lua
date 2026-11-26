-- 水盾。使施术者获得6点灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    GainShield(ctx, "Self", 6)
end

return card
