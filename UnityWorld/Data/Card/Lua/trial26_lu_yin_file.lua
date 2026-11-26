-- 磨锋砂。破除对手 3 灵盾，获得 1 点一次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 3)
    AddArmor(ctx, "Self", 1)
end

return card
