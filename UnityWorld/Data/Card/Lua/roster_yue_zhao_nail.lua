local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local target = self.m_Owner:GetTarget()
    local attack = 3
    if target ~= nil and target.ShieldValue == 0 then attack = 4 end
    Attack(ctx, "Jin", "Ci", attack)
end

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 2)
end

return card
