-- 悬锋重剑。激活期间，由本剑提供的斩招每次赢得实际配对，武器本轮攻击 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Zhan") then return end
    if ctx.SourceCard.ParentCardId ~= self.m_Self.Id then return end
    ChangeEquipmentStat(ctx, "Self", "Attack", 2, 1)
end

return card
