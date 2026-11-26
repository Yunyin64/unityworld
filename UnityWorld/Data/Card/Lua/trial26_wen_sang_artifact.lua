-- 汲露盏。激活期间己方实际受到治疗时，回收一枚未分配灵元作为 MP。
local card = setmetatable({}, { __index = CardBase })

function card:OnHeal(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    Convert(ctx, "Self", "None", 1)
end

return card
