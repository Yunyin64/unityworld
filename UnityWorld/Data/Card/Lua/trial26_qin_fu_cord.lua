-- 托锋穗。己方招式每次实际用到至少 1 枚木灵，获得 1 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not CheckManaUsed(ctx, "Mu", 1) then return end
    GainShield(ctx, "Self", 1)
end

return card
