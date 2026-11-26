-- 容灰罐。激活期间，己方每实际增加 1 层灼烧，获得 1 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnElementStackChanged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckElementStackChange(ctx, "Element_Debuff_Huo", "Increase") then return end
    GainShield(ctx, "Self", ctx:GetObject("Delta"))
end

return card
