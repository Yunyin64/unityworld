-- 理脉结。每次己方刺类招式发动，移除己方 1 层中毒。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Ci") then return end
    RemoveElementBuff(ctx, "Self", "Mu", true, 1)
end

return card
