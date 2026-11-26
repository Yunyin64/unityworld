-- 渡钩。激活期间，己方每次实际移动一张对方的卡，下一次攻击拼点 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnDisplaceOtherCard(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    AddNpcBuff(ctx, "Self", "Attack_Changed_Once", 2, -1)
end

return card
