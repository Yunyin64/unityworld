-- ══════════════════════════════════════════════════════════════
-- 金针
-- 1点金刺，击中本体则下次manaCost减1
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 2,
    CardType = "FaShu",
    ManaCost = { Jin = 1 },
}

card.Keywords = {}

function card:Contest(ctx)
    Attack(ctx, "Jin", "Ci", 1)
end

function card:Apply(ctx)
    -- 拼点赢了 → 自身下次金元素消耗 -1（永久叠加）
    local self_card = ctx.SourceCard
    AddCardStatBuff(ctx, self_card, "ManaAdj_Jin", -1)
end


return card
