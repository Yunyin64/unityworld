local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local equipment = self.m_Self:GetEquipData()
    local defend = equipment:GetObject("Defend") or 0
    Attack(ctx, equipment:GetStringValue("Element", "None"), "Da", defend)
end

return card
