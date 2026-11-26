-- 钳印。激活期间，己方弹药卡发动后获得 2 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Amount") then return end
    GainShield(ctx, "Self", 2)
end

return card
