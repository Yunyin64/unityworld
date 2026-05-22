-- ══════════════════════════════════════════════════════════════
-- 刺拳
-- 基础拳招，造成2点<武器>刺伤
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 3.5,
    CardType = "ZhaoShi",
    ManaCost = {},
}

card.Keywords = {}

function card:Contest(ctx)
    Attack(ctx, "Wu", "Ci", 2)
end


return card