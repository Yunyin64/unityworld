-- ══════════════════════════════════════════════════════════════
--  浩瀚（水·正面）
-- 每次抽灵元时，回复n点MP
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    RecoverMP(ctx, "Self", "None", n)
end

return Buff
