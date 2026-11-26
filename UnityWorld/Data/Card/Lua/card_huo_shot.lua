-- ══════════════════════════════════════════════════════════════
-- 射击。发起5点火系射击拼点。
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 6,
    ManaCost = { Huo = 1 },
}

card.Keywords = {}

function card:Contest(ctx)
    Attack(ctx, "Huo", "SheJi", 5)
end

function card:Apply(ctx)
end

--function card:Tick(ctx)
--end

return card
