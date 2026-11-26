-- 刻墨针。给对手加 1 层出血。一发打完后退场。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddElementBuff(ctx, "Target", "Jin", true, 1)
end

return card
