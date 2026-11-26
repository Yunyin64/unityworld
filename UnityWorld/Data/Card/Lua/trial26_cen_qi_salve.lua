-- 退潮膏。回复 3 MP、治疗 2 HP。用完退场。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    RecoverMP(ctx, "Self", 3)
    Heal(ctx, "Self", 2)
end

return card
