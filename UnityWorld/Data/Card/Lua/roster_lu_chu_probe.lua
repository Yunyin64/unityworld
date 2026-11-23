local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    local target = self.m_Owner:GetTarget()
    if target ~= nil and target.ShieldValue == 0 then attack = attack + 1 end
    Attack(ctx, "Jin", "Ci", attack)
end

return card
