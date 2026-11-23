local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack, _, _, _, element = self:GetEquip()
    local target = self.m_Owner:GetTarget()
    if target ~= nil and target.PendingSlot ~= nil then attack = attack + 1 end
    Attack(ctx, element, "Zhan", attack)
end

return card
