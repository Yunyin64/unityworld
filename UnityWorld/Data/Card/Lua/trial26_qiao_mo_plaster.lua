-- 封脉纸。开场抵消一次金行减益施加。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddElementImmunity(ctx, "Self", "Element_Debuff_Jin", 1)
end

return card
