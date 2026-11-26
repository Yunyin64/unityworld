-- 碎垒功。己方每次主动用效果打空自己的灵盾，下一次打击拼点 +3。
local card = setmetatable({}, { __index = CardBase })

function card:OnShieldExhausted(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckShieldExhaustion(ctx, "Effect") or ctx:GetObject("SourceNpc") ~= self.m_Owner then return end
    AddNpcBuff(ctx, "Self", "Da_Changed_Once", 3, -1)
end

return card
