-- ══════════════════════════════════════════════════════════════
-- 土盾
-- 给自身添加1层护甲，受到伤害时先减去护甲值
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 5,
    CardType = "FaShu",
    ManaCost = { Tu = 1 },
}

card.Keywords = {}

function card:OnContest(ctx)
end

function card:OnApply(ctx)
    local caster = ctx.Caster
    AddNpcBuff(ctx, caster, "buff_armor", 1)
end

function card:OnTick(ctx)
end

return card
