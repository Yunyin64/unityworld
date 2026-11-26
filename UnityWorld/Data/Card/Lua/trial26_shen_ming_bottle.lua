-- 含水瓶。激活期间，每次有己方法术被一枚水灵元充满，下一次火系攻击拼点 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnManaCharged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckManaCharged(ctx, "Shui", "FaShu", true) then return end
    AddNpcBuff(ctx, "Self", "Huo_Changed_Once", 2, -1)
end

return card
