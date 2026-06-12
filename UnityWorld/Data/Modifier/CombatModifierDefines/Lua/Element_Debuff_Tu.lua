-- ══════════════════════════════════════════════════════════════
--  石化（土·负面）
-- 每次抽灵元时，消耗 n 点灵元（ManaConvert 扣 mp）
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    Draw(ctx, "Self", n)
end

return Buff
