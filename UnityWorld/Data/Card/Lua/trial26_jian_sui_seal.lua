-- 隔灰绫。己方每实际清除 1 层灼烧，回复 1 MP。
local card = setmetatable({}, { __index = CardBase })

function card:OnElementStackChanged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckElementStackChange(ctx, "Element_Debuff_Huo", "Decrease") then return end
    RecoverMP(ctx, "Self", -ctx:GetObject("Delta"))
end

return card
