-- 含火盏。激活期间，己方法术每次实付火灵元，下一次水系攻击拼点 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    if not CheckManaUsed(ctx, "Huo", 1) then return end
    AddNpcBuff(ctx, "Self", "Shui_Changed_Once", 2, -1)
end

return card
