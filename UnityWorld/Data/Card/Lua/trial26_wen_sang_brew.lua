-- 回枝汤。治疗 4 HP。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    Heal(ctx, "Self", 4)
end

return card
