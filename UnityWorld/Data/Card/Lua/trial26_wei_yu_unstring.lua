-- 松弦息。回复 1 MP，破对手 2 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    RecoverMP(ctx, "Self", 1)
    ArmorBreak(ctx, "Target", 2)
end

return card
