-- 敌方背包半速10个真实Tick，同类加时。
local card = setmetatable({}, { __index = CardBase })
function card:Apply(ctx)
    Slow(ctx, "Target", 10)
end
return card
