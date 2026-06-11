-- ══════════════════════════════════════════════════════════════
-- 兽肉
-- 被动：战斗开始时气血+1
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 0,
    CardType = "Item",
    ManaCost = {},
}

card.Keywords = { "Passive" }

function card:Apply(ctx)
    AddStatBuff(ctx, "Self", "QiXue", 1, "Flat", "card_monster_meat")
end

return card
