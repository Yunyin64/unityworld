-- 润铆油。回复 1 MP，获得 1 点一次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    RecoverMP(ctx, "Self", 1)
    AddArmor(ctx, "Self", 1)
end

return card
