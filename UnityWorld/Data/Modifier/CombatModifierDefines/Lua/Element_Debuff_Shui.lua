-- ══════════════════════════════════════════════════════════════
--  寒意（水·负面）
-- 每次抽灵元时，己方整包半速 n 秒（每秒10个真实Tick）；同类加时
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    Slow(ctx, "Self", n * 10)
end

return Buff
