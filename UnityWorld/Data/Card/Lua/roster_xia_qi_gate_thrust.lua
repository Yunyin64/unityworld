local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack, _, _, _, element = self:GetEquip()
    local owner = self.m_Owner
    local target = owner:GetTarget()
    if owner.ShieldValue >= 3 or (target ~= nil and target.PendingSlot ~= nil) then attack = attack + 1 end
    Attack(ctx, element, "Ci", attack)
end

return card
