local card = setmetatable({}, { __index = CardBase })

function card:OnDamage(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.armoredRound == round then return end
    local mods = self.m_Owner:GetAllModifiers()
    for i = 0, mods.Count - 1 do
        if mods[i].DefineId == "Armor" then return end
    end
    self.armoredRound = round
    AddArmor(ctx, "Self", 1)
end

return card
