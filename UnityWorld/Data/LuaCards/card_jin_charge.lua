-- ══════════════════════════════════════════════════════════════
-- 加速
-- 充能上方卡牌，使其CD减少1 tick
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 1,
    Cooldown = 5,
    CardType = "FaShu",
    ManaCost = {},
}

card.Keywords = {}

function card:OnContest(ctx)
end

function card:OnApply(ctx)
    -- 找到自身在卡组中的位置，对上方那张卡充能
    local self_card = ctx.SourceCard
    local owner = ctx.Caster
    local idx = self_card:GetDeckIndex()
    if idx > 0 then
        local above = owner:GetCardByIndex(idx - 1)
        Charge(ctx, { above }, 10)
    end
end

function card:OnTick(ctx)
end

return card
