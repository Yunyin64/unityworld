-- 碎印。激活期间，己方每次主动用效果打空自己的灵盾，回复 2 MP。
local card = setmetatable({}, { __index = CardBase })

function card:OnShieldExhausted(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckShieldExhaustion(ctx, "Effect") or ctx:GetObject("SourceNpc") ~= self.m_Owner then return end
    RecoverMP(ctx, "Self", 2)
end

return card
