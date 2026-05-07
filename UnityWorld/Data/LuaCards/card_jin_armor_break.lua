-- ══════════════════════════════════════════════════════════════
-- 破甲
-- 消除对方5点护盾值
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 5,
    CardType = "FaShu",
    ManaCost = {},
}

card.Keywords = {}

function card:OnContest(ctx)
end

function card:OnApply(ctx)
    ArmorBreak(ctx, 5)
end

function card:OnTick(ctx)
end

return card
