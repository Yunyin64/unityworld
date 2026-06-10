-- ══════════════════════════════════════════════════════════════
-- 土缚
-- 减速敌方随机一张卡（CD累计速率降低50%），可叠加
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 5,
    CardType = "FaShu",
    ManaCost = { Tu = 1 },
}

card.Keywords = {}


function card:Apply(ctx)
    Slow(ctx, "TargetRandom", 10)
end


return card
