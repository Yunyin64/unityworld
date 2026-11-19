-- ══════════════════════════════════════════════════════════════
-- 狼牙
-- 被动：战斗开始时获得一层刺击强化(Ci_Improved)
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 0,
    ManaCost = {},
}

function card:Apply(ctx)
    AddNpcBuff(ctx, "Self", "Ci_Improved", 1,-1)
end

return card
