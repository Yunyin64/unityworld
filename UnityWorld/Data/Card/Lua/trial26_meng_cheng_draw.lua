-- 蓄潮。消耗至多 3 MP，按空格和剩余灵气转换为等量灵元。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    Draw(ctx, "Self", 3)
end

return card
