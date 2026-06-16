-- ══════════════════════════════════════════════════════════════
--  中毒（木·负面）
-- 每次抽灵元时，随机给自己添加 n 层负面五行 Buff
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    AddElementBuff(ctx, "Self", "None", true, n)
end

return Buff
