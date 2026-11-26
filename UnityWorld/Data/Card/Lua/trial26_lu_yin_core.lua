-- 藏锋诀。每轮为己方法宝供 1 灵。己方发动带斩或刺关键词的招式时，回复 1 MP。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArtifactPower(ctx, "Self", "None", 1, 0)
end

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Zhan") and not HasKeyword(ctx, ctx.SourceCard, "Ci") then return end
    RecoverMP(ctx, "Self", 1)
end

return card
