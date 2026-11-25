local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    local equipment = self.m_Self:GetEquipData()
    local speed = math.floor(equipment:GetObject("Speed") or 0)
    if speed > 0 then AddNpcBuff(ctx, "Self", "SheJi_Changed_Once", speed, -1) end
end

return card
