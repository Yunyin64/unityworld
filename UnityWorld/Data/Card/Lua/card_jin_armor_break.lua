-- ══════════════════════════════════════════════════════════════
-- 破甲。消除敌方至多5点灵盾。
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 5,
    ManaCost = { Jin = 2 },
}

card.Keywords = {}

function card:Contest(ctx)
end

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 5)
end

--function card:Tick(ctx)
--end

return card
