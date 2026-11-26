-- ══════════════════════════════════════════════════════════════
-- 重斩。发起6点金系斩击拼点。
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 4,
    ManaCost = { Jin = 2 },
}

card.Keywords = {}

function card:Contest(ctx)
    Attack(ctx, "Jin", "Zhan", 6)
end

function card:Apply(ctx)
end

--function card:Tick(ctx)
--end

return card
