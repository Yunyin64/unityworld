local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    local target = self.m_Owner:GetTarget()
    if target ~= nil then
        local field = target:GetField()
        for i = 0, field.Count - 1 do
            if field[i].IsFrozen then
                attack = attack + 1
                break
            end
        end
    end
    Attack(ctx, "Shui", "Da", attack)
end

return card
