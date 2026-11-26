-- 青翎弓。激活期间，由它提供的「瞄」发动时，本轮武器攻击 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local shot = ctx.SourceCard
    if shot == nil or shot.DefineId ~= "card_form_miao" or shot.ParentCardId ~= self.m_Self.Id then return end
    ChangeEquipmentStat(ctx, "Self", "Attack", 2, 1)
end

return card
