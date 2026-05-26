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

function card:Contest(ctx)
    Defend(ctx, "Block", 4)
end


return card
