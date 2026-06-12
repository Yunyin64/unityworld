-- ══════════════════════════════════════════════════════════════
--  寒意（水·负面）
-- 每次抽灵元时，随机减速自己一张卡 n 层
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    Slow(ctx, "Random", n)
end

return Buff
