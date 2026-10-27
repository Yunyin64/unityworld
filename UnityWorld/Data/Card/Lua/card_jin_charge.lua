-- 整包充能10个基准Tick；旧空费用未定，内容保持Unmigrated。
local card = setmetatable({}, { __index = CardBase })
function card:Apply(ctx)
    Charge(ctx, "Self", 10)
end
return card
