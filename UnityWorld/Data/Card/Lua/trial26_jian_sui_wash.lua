-- 洗烬水囊。己方法术每次实付水灵元时，按本次实付的水灵枚数清除自己的灼烧。
local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    if not CheckManaUsed(ctx, "Shui", 1) then return end
    RemoveElementBuff(ctx, "Self", "Huo", true, ctx:GetObject("PaidShui"))
end

return card
