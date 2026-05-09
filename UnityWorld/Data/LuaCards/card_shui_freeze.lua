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
    -- 找敌方一张正在CD中的随机卡牌，冻结10 tick
    local target = ctx.Caster.Target
    local con,targetCard = RandomCardInCD(ctx, target)
    if con then
        Freeze(ctx, targetCard, 10)
    end
end

function card:OnTick(ctx)
end

return card
