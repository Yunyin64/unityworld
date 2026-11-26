-- 爆燃。按默认行距增加两行扫描进度。
local card = setmetatable({}, { __index = CardBase })
function card:Apply(ctx)
    Charge(ctx, "Self", 20)
end
return card
