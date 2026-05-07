-- ══════════════════════════════════════════════════════════════
-- 回旋斩
-- 3点物理斩击，每触发一次攻击卡为自己充能1s（10 tick）
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 8,
    CardType = "FaShu",
    ManaCost = {},
}

card.Keywords = {}

function card:OnContest(ctx)
    Attack(ctx, "Jin", "Zhan", 3)
end

function card:OnApply(ctx)
    -- 每次触发攻击后，为自己充能 1s（10 tick）
    local self_card = ctx.SourceCard
    Charge(ctx, { self_card }, 10)
end

function card:OnTick(ctx)
end

return card
