-- ══════════════════════════════════════════════════════════════
--  浩瀚（水·正面）
-- 每次抽灵元时，将 n 点灵元转化回 MP（ManaConvert 回 mp）
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    Convert(ctx, "Self", "None", n)
end

return Buff
