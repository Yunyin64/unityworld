-- 引灰诀。给自己附加 1 层灼烧，随后尝试用 1 MP 抽 1 灵元。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddElementBuff(ctx, "Self", "Huo", true, 1)
    Draw(ctx, "Self", 1)
end

return card
