-- 回声螺。激活期间，己方每次成功发起打击法术，获得 1 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Da") then return end
    GainShield(ctx, "Self", 1)
end

return card
