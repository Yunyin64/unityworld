-- 量潮珠。每笔己方 MP 实际扣除至少 3 点时，获得 1 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnMPChanged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx:GetObject("Delta") > -3 then return end
    GainShield(ctx, "Self", 1)
end

return card
