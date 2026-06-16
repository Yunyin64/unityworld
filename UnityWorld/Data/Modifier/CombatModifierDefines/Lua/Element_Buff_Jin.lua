-- ══════════════════════════════════════════════════════════════
--  锐意（金·正面）
-- 每次抽灵元时，随机给自己添加 n 层正面五行 Buff
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    AddElementBuff(ctx, "Self", "None", false, n)
end

return Buff
