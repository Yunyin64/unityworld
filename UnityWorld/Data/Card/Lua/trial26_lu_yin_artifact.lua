-- 青脊剑。激活期间，己方成功发动格招，得 2 点一次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Block") then return end
    AddArmor(ctx, "Self", 2)
end

return card
