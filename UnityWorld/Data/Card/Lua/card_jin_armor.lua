-- ══════════════════════════════════════════════════════════════
-- 金甲
-- 发起12点防御拼点（挡）。
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 8,
    ManaCost = { Jin = 1 },
}

card.Keywords = {}

function card:Contest(ctx)
    Defend(ctx, "Shield", 12)
end

function card:Apply(ctx)
end

--function card:Tick(ctx)
--end

return card
