-- ══════════════════════════════════════════════════════════════
-- 回旋斩
-- 3点物理斩击，己方每次攻击时为自己充能10 tick
-- ══════════════════════════════════════════════════════════════

local card = setmetatable({}, { __index = CardBase })

card.CardData = {
    Size = 2,
    Cooldown = 8,
    CardType = "FaShu",
    ManaCost = {},
}

card.Keywords = {}

function card:Contest(ctx)
    Attack(ctx, "Jin", "Zhan", 3)
end

--function card:Apply(ctx)
--end

function card:OnAttack(ctx)
    -- 己方攻击时，为自己充能 10 tick
    local self_card = self.m_Self
    if ctx.Caster == self.m_Owner then
        Charge(ctx,self_card , 10)
    end
end

--function card:Tick(ctx)
--end

return card
