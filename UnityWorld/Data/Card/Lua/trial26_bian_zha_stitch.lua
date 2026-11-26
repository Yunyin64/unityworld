-- 补片针。己方获得普通伤势时，从己方盘面回收至多 2 枚未分配灵元为 MP。
local card = setmetatable({}, { __index = CardBase })

function card:OnWoundGained(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if HasKeyword(ctx, ctx.SourceCard, "Temp") then return end
    if not CheckWoundEvent(ctx, self.m_Owner, "Ally", 1) then return end
    Convert(ctx, "Self", "None", 2)
end

return card
