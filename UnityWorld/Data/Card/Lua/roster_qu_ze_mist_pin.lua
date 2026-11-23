local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local attack = 4
    if self.m_Owner.ShieldValue >= 2 then attack = 5 end
    Attack(ctx, "Shui", "Ci", attack)
end

return card
