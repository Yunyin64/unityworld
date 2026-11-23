local card = setmetatable({}, { __index = CardBase })

function card:OnDisplaceOtherCard(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    local moved = ctx:GetObject("MovedCard")
    local target = self.m_Owner:GetTarget()
    if self.lastRound == round or target == nil or moved == nil or moved.Owner ~= target then return end
    self.lastRound = round

    if target.ShieldValue > 0 then
        ArmorBreak(ctx, "Target", 2)
        return
    end

    local modifiers = self.m_Owner:GetAllModifiers()
    for i = 0, modifiers.Count - 1 do
        if modifiers[i].DefineId == "Attack_Changed_Once" then return end
    end
    local source = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    AddNpcBuff(source, "Self", "Attack_Changed_Once", 1, -1)
end

return card
