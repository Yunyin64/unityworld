-- ══════════════════════════════════════════════════════════════
-- 冻结
-- 冻结敌方随机一张正在CD中的卡牌，暂停其CD 1 tick
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 3,
    CardType = "FaShu",
    ManaCost = { Shui = 1 },
}

card.Keywords = {}

function card:Contest(ctx)
end

function card:Apply(ctx)
    Freeze(ctx, "TargetRandom", 10)
end

--function card:Tick(ctx)
--end

return card
