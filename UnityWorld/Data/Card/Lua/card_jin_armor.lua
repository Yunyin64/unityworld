-- ══════════════════════════════════════════════════════════════
-- 金甲
-- 展开12点金系护盾
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 8,
    CardType = "FaShu",
    ManaCost = { Jin = 2 },
}

card.Keywords = {}

function card:Contest(ctx)
    Shield(ctx, 12)
end

function card:Apply(ctx)
end

function card:OnTick(ctx)
end

return card
