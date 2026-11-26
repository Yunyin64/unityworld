-- 回纹带。每次己方格类招式发动，回复 1 MP。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Block") then return end
    RecoverMP(ctx, "Self", 1)
end

return card
