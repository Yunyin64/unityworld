-- ══════════════════════════════════════════════════════════════
-- 土盾。获得1点护甲，减免下一次来自敌方的伤害。
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 5,
    ManaCost = { Tu = 2 },
}

card.Keywords = {}

--function card:Contest(ctx)
--end

function card:Apply(ctx)
    AddArmor(ctx, "Self", 1)
end

--function card:Tick(ctx)
--end

return card
