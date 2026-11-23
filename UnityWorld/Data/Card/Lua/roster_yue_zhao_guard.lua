local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    local target = self.m_Owner:GetTarget()
    if target ~= nil and target.PendingSlot ~= nil and target.PendingSlot.IsAttackType then defend = defend + 1 end
    Defend(ctx, "Block", defend)
end

return card
