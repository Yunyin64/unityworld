-- 楔锋术。破除对手 2 灵盾，并移除其格上的 1 枚未分配灵元。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 2)
    ReduceMana(ctx, "Target", "None", 1)
end

return card
