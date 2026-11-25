local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local equipment = self.m_Self:GetEquipData()
    local defend = equipment:GetObject("Defend") or 0
    Defend(ctx, "Dodge", defend)
end

return card
