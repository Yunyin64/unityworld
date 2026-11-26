-- 阔刃绳。开场获得 2 点一次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArmor(ctx, "Self", 2)
end

return card
