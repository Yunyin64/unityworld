-- ══════════════════════════════════════════════════════════════
--  石化（土·负面）
-- 每次抽灵元时，减少 n 点MP
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    ReduceMP(ctx, "Self", n)
end

return Buff
