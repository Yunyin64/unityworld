-- ══════════════════════════════════════════════════════════════
-- 爆燃
-- 全卡加速1s（10 tick）
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 10,
    CardType = "FaShu",
    ManaCost = { Huo = 2 },
}

card.Keywords = {}

function card:Contest(ctx)
end

function card:Apply(ctx)
    Charge(ctx, "All", 10)
end

--function card:Tick(ctx)
--end

return card
