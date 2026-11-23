local card = setmetatable({}, { __index = CardBase })

function card:OnStraight(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not ctx.SourceCard:GetTags():Contains("防御") then return end
    self.used = true
    RecoverMP(ctx, "Self", 2)
end

return card
