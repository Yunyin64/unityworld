local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local attack = 3
    local field = self.m_Owner:GetField()
    for i = 0, field.Count - 1 do
        if HasKeyword(ctx, field[i], "Wound") then
            attack = attack + 1
            break
        end
    end
    Attack(ctx, "Jin", "Ci", attack)
end

return card
