-- 筛灵诀。每次己方基础补灵前，回收盘面最多 2 枚未分配灵元为 MP。
local card = setmetatable({}, { __index = CardBase })

function card:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    Convert(ctx, "Self", "None", 2)
end

return card
