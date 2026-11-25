local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    local equipment = self.m_Self:GetEquipData()
    local speed = math.floor(equipment:GetObject("Speed") or 0)
    Convert(ctx, "Self", "None", speed)
end

return card
