-- ══════════════════════════════════════════════════════════════
-- 斩击
-- 金系斩击，造成2点金斩伤害
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 2,
    CardType = "FaShu",
    ManaCost = { Jin = 1 },
}

card.Keywords = {}

function card:OnContest(ctx)
    Attack(ctx, "Jin", "Zhan", 2)
end

function card:OnApply(ctx)
end

function card:OnTick(ctx)
end

return card
