-- 蹬灯履。开场给本方扫描进度充 4 Tick。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    Charge(ctx, "Self", 4)
end

return card
