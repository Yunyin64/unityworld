local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local target = self.m_Owner:GetTarget()
    local attack = 4
    if target ~= nil and target.PendingSlot == nil then attack = 5 end
    Attack(ctx, "Tu", "SheJi", attack)
end

return card
