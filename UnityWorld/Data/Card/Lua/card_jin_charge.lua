-- 加速。己方背包以双倍速度扫描1秒。
local card = setmetatable({}, { __index = CardBase })
function card:Apply(ctx)
    Haste(ctx, "Self", 10)
end
return card
