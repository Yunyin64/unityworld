local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local kind = "SheJi"
    local target = self.m_Owner:GetTarget()
    if target ~= nil and target.PendingSlot ~= nil and target.PendingSlot.IsDefenseType then kind = "Ci" end
    Attack(ctx, "Shui", kind, 5)
end

return card
