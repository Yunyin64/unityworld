-- 疗愈。只从恰好占1格的己方伤势中抽选。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    RemoveRandomWound(ctx, "Self", 1, true)
end

return card
