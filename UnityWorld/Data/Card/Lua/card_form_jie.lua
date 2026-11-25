local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local equipment = self.m_Self:GetEquipData()
    local attack = equipment:GetObject("Attack") or 0
    Defend(ctx, "Block", attack)
end

return card
