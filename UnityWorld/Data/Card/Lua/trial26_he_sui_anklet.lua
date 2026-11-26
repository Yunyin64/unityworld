-- 压踝缚。己方闪避每次赢过攻击拼点，获得 1 点一次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx:GetStringValue("WinnerTypeName", "") ~= "Dodge" then return end
    local rival = ctx:GetStringValue("LoserTypeName", "")
    if rival ~= "Zhan" and rival ~= "Ci" and rival ~= "Da" and rival ~= "SheJi" then return end
    AddArmor(ctx, "Self", 1)
end

return card
