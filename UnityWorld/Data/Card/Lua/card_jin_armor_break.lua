-- ══════════════════════════════════════════════════════════════
-- 破甲
-- 消除对方5点护盾值
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 5,
    CardType = "FaShu",
    ManaCost = {},
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
