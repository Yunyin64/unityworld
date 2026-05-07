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

function card:OnContest(ctx)
end

function card:OnApply(ctx)
    -- 减速敌方随机一张卡
    local target = ctx.Caster.Target
    if target == nil then return end
    local allCards = target:GetAllCards()
    if allCards == nil or allCards.Count == 0 then return end
    local idx = math.random(0, allCards.Count - 1)
    local targetCard = allCards[idx]
    Slow(ctx)
end

function card:OnTick(ctx)
end

return card
