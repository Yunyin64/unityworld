local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local attack = 4
    if self.m_Owner.ShieldValue == 0 then attack = 5 end
    Attack(ctx, "Jin", "Ci", attack)
end

return card
