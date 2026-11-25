local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local equipment = self.m_Self:GetEquipData()
    local speed = math.floor(equipment:GetObject("Speed") or 0)
    Defend(ctx, "Shield", speed)
end

return card
