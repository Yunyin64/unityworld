-- ══════════════════════════════════════════════════════════════
-- 格挡。发起4点格，胜出奖励由 JSON 配置的 Block 关键词结算。
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 3,
    ManaCost = { Tu = 1 },
}

card.Keywords = {}

function card:Contest(ctx)
    Defend(ctx, "Block", 4)
end


return card
