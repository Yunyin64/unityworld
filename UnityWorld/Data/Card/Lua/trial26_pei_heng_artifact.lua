-- 穿水枪。激活期间，本装备每次有招式充满，武器本轮攻击 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnManaCharged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckManaCharged(ctx, "Any", "ZhaoShi", true) then return end
    if ctx.SourceCard.ParentCardId ~= self.m_Self.Id then return end
    ChangeEquipmentStat(ctx, "Self", "Attack", 2, 1)
end

return card
