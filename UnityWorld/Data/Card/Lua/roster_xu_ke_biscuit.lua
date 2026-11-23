local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    if self.m_Owner.ShieldValue < 3 then GainShield(ctx, "Self", 2) end
    Heal(ctx, "Self", 1)
end

return card
