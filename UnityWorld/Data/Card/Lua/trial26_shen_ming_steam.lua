-- 回温诀。解冻正上方共边的己方法宝，回复 1 MP。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    SelectDirectionalCards(ctx, "Up", "FaBao")
    ThawCard(ctx, "Selected")
    RecoverMP(ctx, "Self", 1)
end

return card
