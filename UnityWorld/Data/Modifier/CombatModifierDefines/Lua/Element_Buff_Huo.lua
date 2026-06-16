-- ══════════════════════════════════════════════════════════════
--  心火（火·正面）
-- 每次抽灵元时，随机加速自己一张卡 n 层
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    Haste(ctx, "Random", n)
end

return Buff
