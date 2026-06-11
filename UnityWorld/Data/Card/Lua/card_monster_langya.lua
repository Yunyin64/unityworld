-- ══════════════════════════════════════════════════════════════
-- 狼牙
-- 被动：战斗开始时获得一层刺击强化(CiDmgFlat)
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
    AddNpcBuff(ctx, "Self", "CiDmgFlat", 1,-1)
end

return card
