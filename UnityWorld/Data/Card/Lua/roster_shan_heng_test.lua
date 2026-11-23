local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local kind = "SheJi"
    local target = self.m_Owner:GetTarget()
    if target ~= nil and target.ShieldValue >= 2 then kind = "Zhan" end
    Attack(ctx, "Jin", kind, 4)
end

return card
