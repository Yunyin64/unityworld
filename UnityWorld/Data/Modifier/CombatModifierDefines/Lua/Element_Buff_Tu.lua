-- ══════════════════════════════════════════════════════════════
--  载德（土·正面）
-- 每次抽灵元时，随机清除自己 n 层负面五行 Buff
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:OnManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local n = self.m_Self.CurrentStack
    RemoveElementBuff(ctx, "Self", "None", true, n)
end

return Buff
