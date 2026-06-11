-- ══════════════════════════════════════════════════════════════
--  斩击抗性
-- 每层：受到斩击伤害 -1
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:ModifyDamageIn(ctx)
    local dmg = ctx:GetObject("DamageInfo")
    if dmg.damageType == DamageType.Zhan then
        dmg.Damage = dmg.Damage - self.m_Self.CurrentStack
        if dmg.Damage < 0 then dmg.Damage = 0 end
    end
end

return Buff
