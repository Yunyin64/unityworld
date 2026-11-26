-- 回潮经。己方灵盾每次被敌方伤害打空，下次打击拼点 +3。
local card = setmetatable({}, { __index = CardBase })

function card:OnShieldExhausted(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckShieldExhaustion(ctx, "Damage") then return end
    if ctx:GetObject("SourceNpc") == nil or ctx:GetObject("SourceNpc").Team == self.m_Owner.Team then return end
    AddNpcBuff(ctx, "Self", "Da_Changed_Once", 3, -1)
end

return card
