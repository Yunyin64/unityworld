-- 回旋斩。持有者每发动一次攻击，为整包充能 1 Tick。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "Zhan", 3)
end

function card:OnAttack(ctx)
    if ctx.Caster == self.m_Owner then
        Charge(ctx, "Self", 1)
    end
end

return card
