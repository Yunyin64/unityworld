-- 窄门鉴。激活期间，每当己方攻击法术进入直击结算，先破除对手 2 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnStraight(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    local kind = ctx:GetStringValue("ContestType", "")
    if kind ~= "Zhan" and kind ~= "Ci" and kind ~= "Da" and kind ~= "SheJi" then return end
    ArmorBreak(ctx, "Target", 2)
end

return card
