-- 回复。为施术者回复2点HP。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    Heal(ctx, "Self", 2)
end

return card
