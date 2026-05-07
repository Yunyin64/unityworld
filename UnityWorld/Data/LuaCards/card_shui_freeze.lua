-- ══════════════════════════════════════════════════════════════
-- 冻结
-- 冻结敌方随机一张正在CD中的卡牌，暂停其CD 1 tick
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 3,
    CardType = "FaShu",
    ManaCost = { Shui = 1 },
}

card.Keywords = {}

function card:OnContest(ctx)
end

function card:OnApply(ctx)
    -- 找敌方一张正在CD中的随机卡牌，冻结1 tick
    local target = ctx.Caster.Target
    if target == nil then return end
    local cdCards = target:GetCardsInCD()
    if cdCards == nil or cdCards.Count == 0 then return end
    local idx = math.random(0, cdCards.Count - 1)
    local targetCard = cdCards[idx]
    Freeze(ctx, targetCard, 1)
end

function card:OnTick(ctx)
end

return card
