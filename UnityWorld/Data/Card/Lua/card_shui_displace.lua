-- 流动。随机选一张能下滑的敌方卡牌。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    SlideCard(ctx, "TargetRandom", "Down")
end

return card
