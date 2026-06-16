-- ══════════════════════════════════════════════════════════════
--  出血（金·负面）
-- 每次抽灵元时，对自己造成 n 点伤害
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    SelfDamage(ctx, "Self", n)
end

return Buff
