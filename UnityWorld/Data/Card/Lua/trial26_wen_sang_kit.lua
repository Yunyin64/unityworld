-- 卷枝药匣。移除一张随机的不超过 2 格伤势。药匣有 2 贴，用完退场。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    RemoveRandomWound(ctx, "Self", 2, false)
end

return card
