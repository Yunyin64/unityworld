-- 退岸潮。选择对手一张能向下滑的牌，使它向下滑到阻挡前。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    SlideCard(ctx, "TargetRandom", "Down")
end

return card
