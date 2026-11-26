-- 折垒环。每次己方打击法术发动，移除自己至多 5 灵盾，再破除对手 2 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Da") then return end
    ArmorBreak(ctx, "Self", 5)
    ArmorBreak(ctx, "Target", 2)
end

return card
