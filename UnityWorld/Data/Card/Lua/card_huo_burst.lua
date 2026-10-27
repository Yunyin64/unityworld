-- 己方背包增加10个基准Tick进度，不受加减速缩放。
local card = setmetatable({}, { __index = CardBase })
function card:Apply(ctx)
    Charge(ctx, "Self", 10)
end
return card
