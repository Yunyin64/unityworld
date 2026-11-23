local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    local mods = self.m_Owner:GetAllModifiers()
    for i = 0, mods.Count - 1 do
        if mods[i].DefineId == "Armor" then
            attack = attack + 1
            break
        end
    end
    Attack(ctx, "Shui", "Da", attack)
end

return card
