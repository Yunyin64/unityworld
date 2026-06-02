-- ══════════════════════════════════════════════════════════════
-- 回灵
-- 立刻抽取3点MP转化为灵元
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 5,
    CardType = "FaShu",
    ManaCost = { Huo = 1 },
}

card.Keywords = {}

function card:Contest(ctx)
end

function card:Apply(ctx)
    Draw(ctx, 3)
end

--function card:Tick(ctx)
--end

return card
