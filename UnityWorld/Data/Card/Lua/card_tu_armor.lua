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

--function card:Contest(ctx)
--end

function card:Apply(ctx)
    AddNpcBuff(ctx, "Self", "buff_armor", 1,-1)
end

--function card:Tick(ctx)
--end

return card
