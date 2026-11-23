local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    Attack(ctx, "Shui", "Da", attack)
end

function card:Apply(ctx)
    if self.m_Owner:GetMp() < 18 then RecoverMP(ctx, "Self", 1) end
end

return card
