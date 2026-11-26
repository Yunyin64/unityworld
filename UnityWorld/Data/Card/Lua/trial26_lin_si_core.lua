-- 守隙经。己方每次攻击法术进入直击结算，下一次攻击拼点 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnStraight(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    local kind = ctx:GetStringValue("ContestType", "")
    if kind ~= "Zhan" and kind ~= "Ci" and kind ~= "Da" and kind ~= "SheJi" then return end
    AddNpcBuff(ctx, "Self", "Attack_Changed_Once", 2, -1)
end

return card
