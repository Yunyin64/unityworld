-- ══════════════════════════════════════════════════════════════
-- 格挡
-- 土系格挡4点
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 3,
    CardType = "FaShu",
    ManaCost = {},
}

card.Keywords = {}

function card:OnContest(ctx)
    Block(ctx, 4)
end

function card:OnApply(ctx)
end

function card:OnTick(ctx)
end

return card
