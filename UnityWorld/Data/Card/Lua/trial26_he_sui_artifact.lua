-- 斜纹靴。激活期间，由本装备提供的闪避招式每次发动，获得 2 点卸力。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Dodge") then return end
    if ctx.SourceCard.ParentCardId ~= self.m_Self.Id then return end
    AddDeflection(ctx, "Self", 2)
end

return card
