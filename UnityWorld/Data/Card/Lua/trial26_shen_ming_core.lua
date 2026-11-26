-- 双盏调息法。每轮给己方法宝供 1 枚水灵元。法宝每次实付至少 1 枚火灵，回复 1 MP。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArtifactPower(ctx, "Self", "Shui", 1, 0)
end

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaBao") then return end
    if not CheckManaUsed(ctx, "Huo", 1) then return end
    RecoverMP(ctx, "Self", 1)
end

return card
