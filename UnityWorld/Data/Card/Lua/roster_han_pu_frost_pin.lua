local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local attack = 4
    local target = self.m_Owner:GetTarget()
    if target ~= nil then
        if target:GetCombatHpMax() - target:GetHp() >= 4 then attack = 5 end
        local field = target:GetField()
        for i = 0, field.Count - 1 do
            if field[i].IsFrozen then
                attack = attack + 1
                break
            end
        end
    end
    Attack(ctx, "Shui", "Ci", attack)
end

return card
