-- ══════════════════════════════════════════════════════════════
-- 金锋斩。发起2点金系斩击拼点。
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 2,
    ManaCost = { Jin = 1 },
}

card.Keywords = {}

function card:Contest(ctx)
    Attack(ctx, "Jin", "Zhan", 2)
end

function card:Apply(ctx)
end

--function card:Tick(ctx)
--end

return card
