-- ══════════════════════════════════════════════════════════════
-- 射击
-- 火系射击，造成5点火射伤害
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 6,
    CardType = "FaShu",
    ManaCost = { Huo = 1 },
}

card.Keywords = {}

function card:OnContest(ctx)
    Attack(ctx, "Huo", "SheJi", 5)
end

function card:OnApply(ctx)
end

function card:OnTick(ctx)
end

return card
