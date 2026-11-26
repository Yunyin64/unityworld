-- 风膛匣。激活期间，己方法术实付火灵元时，加速 5 Tick。
local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    if not CheckManaUsed(ctx, "Huo", 1) then return end
    Haste(ctx, "Self", 5)
end

return card
