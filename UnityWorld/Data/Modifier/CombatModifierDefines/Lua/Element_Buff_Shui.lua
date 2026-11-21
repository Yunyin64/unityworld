-- ══════════════════════════════════════════════════════════════
--  浩瀚（水·正面）
-- 每次基础补灵前，按层回复 MP
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    RecoverMP(ctx, "Self", n)
end

return Buff
