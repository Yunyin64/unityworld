-- 薄水堤。获得 5 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    GainShield(ctx, "Self", 5)
end

return card
