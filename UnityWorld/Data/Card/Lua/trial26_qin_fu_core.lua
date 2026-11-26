-- 悬锋诀。每轮给一件法宝供 2 灵。己方每次发动「蓄」，回复 2 MP。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArtifactPower(ctx, "Self", "None", 2, 0)
end

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx.SourceCard == nil or ctx.SourceCard.DefineId ~= "card_form_xu" then return end
    RecoverMP(ctx, "Self", 2)
end

return card
