local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, value = self:GetEquip()
    local target = self.m_Owner:GetTarget()
    if target ~= nil then
        local mods = target:GetAllModifiers()
        for i = 0, mods.Count - 1 do
            if mods[i].DefineId == "Roster_Suture" then
                value = value + 1
                break
            end
        end
    end
    Attack(ctx, "Jin", "Ci", value)
end

return card
