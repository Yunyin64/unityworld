-- 磐壁盾。激活期间，由本装备提供的格招每次发动，武器本轮防御 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Block") then return end
    if ctx.SourceCard.ParentCardId ~= self.m_Self.Id then return end
    ChangeEquipmentStat(ctx, "Self", "Defend", 2, 1)
end

return card
