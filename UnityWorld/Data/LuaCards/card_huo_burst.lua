-- ══════════════════════════════════════════════════════════════
-- 爆燃
-- 全卡加速1s（10 tick）
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 10,
    CardType = "FaShu",
    ManaCost = { Huo = 2 },
}

card.Keywords = {}

function card:OnContest(ctx)
end

function card:OnApply(ctx)
    -- 对自身所有卡牌充能 10 tick（1s）
    local owner = ctx.Caster
    local con,allCards = AllCard(ctx, owner)
    if con then
        Charge(ctx, allCards, 10)
    end
end

function card:OnTick(ctx)
end

return card
