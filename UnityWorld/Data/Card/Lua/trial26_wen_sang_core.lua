-- 续枝诀。己方每次实际回复至少 2 HP，下一次木系攻击拼点 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnHeal(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx:GetObject("Amount") < 2 then return end
    AddNpcBuff(ctx, "Self", "Mu_Changed_Once", 2, -1)
end

return card
