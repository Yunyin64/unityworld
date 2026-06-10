-- ══════════════════════════════════════════════════════════════
-- 加速
-- 充能上方卡牌，使其CD减少1 tick
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 5,
    CardType = "FaShu",
    ManaCost = {},
}

card.Keywords = {}

function card:Contest(ctx)
end

function card:Apply(ctx)
    Charge(ctx, "AboveAll", 10)
end

--function card:Tick(ctx)
--end

return card
