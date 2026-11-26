-- ══════════════════════════════════════════════════════════════
-- 金针
-- 金刺初始1点。每次实际扣除敌方生命后，本场后续金刺的拼点提高1点。
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })
local stabValue = 1

card.CardData = {
    Size = 1,
    Cooldown = 2,
    ManaCost = { Jin = 1 },
}

card.Keywords = {}

function card:Contest(ctx)
    Attack(ctx, "Jin", "Ci", stabValue)
end

function card:OnDamage(ctx)
    if ctx.SourceCard ~= self.m_Self or ctx.Caster.Team == self.m_Owner.Team then return end
    stabValue = stabValue + 1
end


return card
