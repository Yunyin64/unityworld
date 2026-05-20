-- ══════════════════════════════════════════════════════════════
-- 土缚
-- 减速敌方随机一张卡（CD累计速率降低50%），可叠加
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 5,
    CardType = "FaShu",
    ManaCost = { Tu = 1 },
}

card.Keywords = {}


function card:Apply(ctx)
    -- 减速敌方随机一张卡
    local target = ctx.Caster.Target
    local con, targetCard = RandomCardInCD(ctx, target)
    if con then
        Slow(ctx, targetCard, 1)
    end
end


return card
