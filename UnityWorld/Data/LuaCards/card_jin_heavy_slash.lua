-- ══════════════════════════════════════════════════════════════
-- 重斩
-- 金系重斩，造成6点金斩伤害
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 4,
    CardType = "FaShu",
    ManaCost = { Jin = 1 },
}

card.Keywords = {}

function card:OnContest(ctx)
    Attack(ctx, "Jin", "Zhan", 6)
end

function card:OnApply(ctx)
end

function card:OnTick(ctx)
end

return card
