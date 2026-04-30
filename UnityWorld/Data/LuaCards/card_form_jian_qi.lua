-- ══════════════════════════════════════════════════════════════
-- 剑气
-- 基础剑招，造成3点<武器>射伤
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 5.5,
    CardType = "ZhaoShi",
    ManaCost = {},
}

card.Keywords = {}

function card:OnContest(ctx)
    Attack(ctx, "Wu", "SheJi", 3)
end

function card:OnApply(ctx)
end

function card:OnTick(ctx)
end

return card