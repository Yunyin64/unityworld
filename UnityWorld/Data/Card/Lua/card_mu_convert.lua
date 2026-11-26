-- 转化。回收己方格上至多1个任意元素的未分配灵元。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    Convert(ctx, "Self", "None", 1)
end

return card
