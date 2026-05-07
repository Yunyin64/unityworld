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

function card:OnContest(ctx)
    Attack(ctx, "Jin", "Ci", 1)
end

function card:OnApply(ctx)
    -- 击中本体（造成伤害生效）→ 下次 ManaCost 减 1
    -- 通过 Stat 修改器临时降低金元消耗
    local self_card = ctx.SourceCard
    if self_card then
        self_card.Stats:Add("ManaAdj_Jin", -1)
    end
end

function card:OnTick(ctx)
end

return card
