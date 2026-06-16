-- ══════════════════════════════════════════════════════════════
--  再生（木·正面）
-- 每次抽灵元时，回复 n 点 HP
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    Heal(ctx, "Self", n)
end

return Buff
