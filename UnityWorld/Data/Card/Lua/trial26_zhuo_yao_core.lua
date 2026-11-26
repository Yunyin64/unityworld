-- 并薪诀。己方法术实付至少两种灵元时，回复 1 MP。
local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    if not CheckManaUsed(ctx, "Kinds", 2) then return end
    RecoverMP(ctx, "Self", 1)
end

return card
