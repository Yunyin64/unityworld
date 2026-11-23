-- ══════════════════════════════════════════════════════════════
--  兽皮
-- 被动：战斗开始时获得一层斩击抗性（Zhan_DamageIn_Changed 记为 -1 层）。
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 0,
    ManaCost = {},
}

function card:Apply(ctx)
    AddNpcBuff(ctx, "Self", "Zhan_DamageIn_Changed", -1, -1)
end

return card
