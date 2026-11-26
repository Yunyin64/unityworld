-- 洗烬诀。己方每实际移除 1 层灼烧，下一次火系攻击拼点 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnElementStackChanged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckElementStackChange(ctx, "Element_Debuff_Huo", "Decrease") then return end
    AddNpcBuff(ctx, "Self", "Huo_Changed_Once", -2 * ctx:GetObject("Delta"), -1)
end

return card
