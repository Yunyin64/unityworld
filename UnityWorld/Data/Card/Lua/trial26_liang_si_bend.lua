-- 沿脊砂。破除对手 2 灵盾，并回复 1 MP。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 2)
    RecoverMP(ctx, "Self", 1)
end

return card
