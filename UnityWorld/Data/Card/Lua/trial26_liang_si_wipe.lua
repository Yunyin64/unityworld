-- 擦刃术。获得 1 次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArmor(ctx, "Self", 1)
end

return card
