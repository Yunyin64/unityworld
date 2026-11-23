local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    local field = self.m_Owner:GetField()
    for i = 0, field.Count - 1 do
        if HasKeyword(ctx, field[i], "Wound") then
            attack = attack + 1
            break
        end
    end
    Attack(ctx, "Mu", "Ci", attack)
end

return card
