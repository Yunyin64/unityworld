local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Tu", "Zhan", 3)
end

function card:Apply(ctx)
    if self.m_Owner.ShieldValue == 0 then GainShield(ctx, "Self", 1) end
end

return card
