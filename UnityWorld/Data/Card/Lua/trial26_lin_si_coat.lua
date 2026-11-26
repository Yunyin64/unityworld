-- 听雨氅。对手以攻击进入直击时，己方获得 1 点一次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:OnStraight(ctx)
    if ctx.Caster == nil or ctx.Caster.Team == self.m_Owner.Team then return end
    local kind = ctx:GetStringValue("ContestType", "")
    if kind ~= "Zhan" and kind ~= "Ci" and kind ~= "Da" and kind ~= "SheJi" then return end
    AddArmor(ctx, "Target", 1)
end

return card
