-- 己方背包双速10个真实Tick，同类加时。
local card = setmetatable({}, { __index = CardBase })
function card:Apply(ctx)
    Haste(ctx, "Self", 10)
end
return card
