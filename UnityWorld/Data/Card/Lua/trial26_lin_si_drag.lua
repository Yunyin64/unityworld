-- 绊帘术。使对手半速扫描 6 Tick。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    Slow(ctx, "Target", 6)
end

return card
