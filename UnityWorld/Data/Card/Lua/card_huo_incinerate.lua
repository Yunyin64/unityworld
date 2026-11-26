-- 焚烧。回收己方格上的木灵元，转为等量MP。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    Convert(ctx, "Self", "Mu", 1)
end

return card
