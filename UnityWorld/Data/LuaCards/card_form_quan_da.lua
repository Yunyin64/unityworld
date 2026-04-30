-- ══════════════════════════════════════════════════════════════
-- 拳打
-- 基础拳招，造成2点<武器>打伤
-- ══════════════════════════════════════════════════════════════

-- ── 继承元表 ──────────────────────────────────────────
local card = setmetatable({}, { __index = CardBase })

-- ── 数据表 ────────────────────────────────────────────
card.CardData = {
    Size = 1,
    Cooldown = 4,
    CardType = "ZhaoShi",
    ManaCost = {},
}

card.Keywords = {}

-- ── OnXxx 函数 ────────────────────────────────────────

--- 使用时构造拼点
function card:OnContest(ctx)
    Attack(ctx, "Wu", "Da", 2)
end

--- 生效时
function card:OnApply(ctx)
end

--- 每帧逻辑
function card:OnTick(ctx)
end

return card